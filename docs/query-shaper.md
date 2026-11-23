# QueryShaper (`Steverzag.QueryShaper`)

**Purpose:** turn a *data request*, meaning filters, ordering, paging, grouping/aggregates and output
mapping, into one composed `IQueryable<T>` that the database executes.

**Use it when** endpoints serve grids, lists, searches or reports whose shape is chosen by the
client. It replaces hand-written `if (request.SortBy == "name") …` plumbing.

**Non-goals:**
- An ORM, or a replacement for repositories.
- Writes or commands. QueryShaper only reads.
- Defining the selector language. That belongs to [PropertySelectors](property-selectors.md).

## Building blocks

| Feature | Entry point | Input model |
|---|---|---|
| Logic filters | `query.ApplyLogicsFilter(nodes)` | `FilterNode { FieldSelector, FilterCondition, SearchValue, FilterLogicOption, Filters[] }` |
| Base (global) search | `query.ApplyBaseFilter(searchValue[, config])` or `ApplyBaseFilter(lookups, value)` | configured with `ConfigureSearch<T>(…)` |
| Combined | `query.ApplyFilters(lookups, searchValue, nodes)` | |
| Ordering | `query.ApplyOrder(orderParameters)` | `OrderParameters(string OrderBy, OrderDirection)` |
| Pagination | `query.Paginate(page, size)` (sync, core) / `query.PaginateAsync(page, size, ct)` (from `Steverzag.QueryShaper.EntityFrameworkCore`) | |
| Grouping / aggregation | `query.ApplyGrouping(keys).ApplySelect(aggregates)` | `GroupByKeys`, `SelectResult` |
| Mapping | `query.ApplySelect<TIn, TOut>(mapper?)` | `AddMap<TIn,TOut>(m => m.ForMember(d => d.X, "Selector"))` |

`FilterCondition` values: `Equals`, `NotEquals`, `GreaterThan(OrEqual)`, `LessThan(OrEqual)`,
`StartsWith`, `DoesNotStartWith`, `EndsWith`, `DoesNotEndWith`, `IsEmpty`, `IsNotEmpty`, `In`, `NotIn`,
`Contains`, `DoesNotContain`, `ContainsAny`, `DoesNotContainAny`, `ContainsAll`, `DoesNotContainAll`.

## Combining filters

Each `FilterNode` carries a `FilterLogicOption` (`And` / `Or`) that says how it joins **everything
before it**:

- **Strictly left to right, with no precedence.** `A`, `Or B`, `And C` means `(A or B) and C`, not
  `A or (B and C)` as in SQL or C#. The first node's connector is ignored.
- **Groups are parentheses.** A node with child `Filters` is a group. Its children combine with the
  same rules, and its own connector joins the whole group to what came before. Groups nest to any depth.
  To get `A or (B and C)`, write `A`, `Or [group: B, And C]`.
- **A group's own condition is ignored.** If a node has children, its `FieldSelector`,
  `FilterCondition` and `SearchValue` are not used. A node whose `Filters` is empty acts as a plain filter.
- An empty list leaves the query unchanged; a `null` list throws `ArgumentNullException`.

These rules are pinned by `tests/Steverzag.QueryShaper.Tests/Filtering/FilterNodeLogicTests.cs`.

## Grouping and aggregates

`ApplyGrouping(keys)` groups by up to 21 selectors. `ApplySelect(selectors)` on the grouped query then returns
one `SelectResult` per group, and its selectors resolve against the **group**:

| Selector | Means |
|---|---|
| `Key.GroupKey0`, `Key.GroupKey1`, … | the value of the 1st, 2nd, … grouping key (keys are not returned unless selected) |
| `$FN_AGGR_COUNT` | number of rows in the group |
| `Quantity\|$FN_AGGR_SUM` (also `MIN`, `MAX`, `AVG`) | aggregate of a property over the group's rows |
| `Reviews.Stars\|$FN_AGGR_AVG` | aggregate over a nested collection, flattened across the group |

Each `SelectValue.Selector` is the selector text exactly as passed in.

```csharp
var totals = db.Products
    .ApplyGrouping(["Category.Name"])
    .ApplySelect(["Key.GroupKey0", "$FN_AGGR_COUNT", "Quantity|$FN_AGGR_SUM"])
    .ToList();
```

## Example

```csharp
var page = await db.Persons
    .ApplyLogicsFilter([
        new() { FieldSelector = "Phones.PhoneType", FilterCondition = FilterCondition.ContainsAll,
                SearchValue = new List<object> { PhoneType.Home, PhoneType.Work } },
        new() { FieldSelector = "Phones.Id|" + SelectorDefaults.AvgAggregateFunction,
                FilterCondition = FilterCondition.GreaterThan, SearchValue = 100,
                FilterLogicOption = LogicalConnector.And },
    ])
    .ApplyOrder([new OrderParameters("Name", OrderDirection.Ascending)])
    .PaginateAsync(1, 50);
```

## Configuration

```csharp
public class AppQueryShaperConfig : QueryShaperConfiguration
{
    protected override void OnConfiguring(QueryShaperConfigurationBuilder b) => b
        .AddMap<AppPerson, PersonDto>(m => m.ForMember(d => d.PhoneCount, "Phones|$FN_AGGR_COUNT"))
        .ConfigureSearch<AppPerson>(["Name", "Email", "Address.State"]);
}

services.AddQueryShaper<AppQueryShaperConfig>();   // multiple configurations merge into one registry
```

## Status

This is a prototype. Filtering, ordering, paging, grouping and select are covered by tests; mapping
and base search are not yet. The public API is not final. See
[ROADMAP.md](ROADMAP.md) Phases 1, 2 and 4 for the path to a library.
