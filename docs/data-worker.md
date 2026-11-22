# DataWorker (lab name: ExpDataWorkerBeta)

**Purpose:** turn a *data request*, meaning filters, ordering, paging, grouping/aggregates and output
mapping, into one composed `IQueryable<T>` that the database executes.

**Use it when** endpoints serve grids, lists, searches or reports whose shape is chosen by the
client. It replaces hand-written `if (request.SortBy == "name") …` plumbing.

**Non-goals:**
- An ORM, or a replacement for repositories.
- Writes or commands. DataWorker only reads.
- Defining the selector language. That belongs to [PropertySelectors](property-selectors.md).

## Building blocks

| Feature | Entry point | Input model |
|---|---|---|
| Logic filters | `query.ApplyLogicsFilter(nodes)` | `FilterNode { FieldSelector, FilterCondition, SearchValue, FilterLogicOption, Filters[] }` |
| Base (global) search | `query.ApplyBaseFilter(searchValue[, config])` or `ApplyBaseFilter(lookups, value)` | configured with `ConfigureSearch<T>(…)` |
| Combined | `query.ApplyFilters(lookups, searchValue, nodes)` | |
| Ordering | `query.ApplyOrder(orderParameters)` | `OrderParameters(string OrderBy, OrderDirection)` |
| Pagination | `query.PaginateAsync(page, size)` / `PaginatedList<T>.Create` | |
| Grouping / aggregation | `query.ApplyGrouping(keys).ApplySelect(aggregates)` | `GroupByKeys`, `SelectResult` |
| Mapping | `query.ApplySelect<TIn, TOut>(mapper?)` | `AddMap<TIn,TOut>(m => m.ForMember(d => d.X, "Selector"))` |

`FilterCondition` values: `Equals`, `NotEquals`, `GreaterThan(OrEqual)`, `LessThan(OrEqual)`,
`StartsWith`, `DoesNotStartWith`, `EndsWith`, `DoesNotEndWith`, `IsEmpty`, `IsNotEmpty`, `In`, `NotIn`,
`Contains`, `DoesNotContain`, `ContainsAny`, `DoesNotContainAny`, `ContainsAll`, `DoesNotContainAll`.

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
public class AppDataWorkerConfig : DataWorkerConfiguration
{
    protected override void OnConfiguring(DataWorkerConfigurationBuilder b) => b
        .AddMap<AppPerson, PersonDto>(m => m.ForMember(d => d.PhoneCount, "Phones|$FN_AGGR_COUNT"))
        .ConfigureSearch<AppPerson>(["Name", "Email", "Address.State"]);
}

services.AddDataWorker<AppDataWorkerConfig>();   // multiple configurations merge into one registry
```

## Status

This is a prototype. It has no tests yet, and the public API is not final. See
[ROADMAP.md](ROADMAP.md) Phases 1, 2 and 4 for the path to a library.
