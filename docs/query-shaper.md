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
| Logic filters | `query.ApplyLogicsFilter(nodes)` | `FilterNode` = `ConditionNode { FieldSelector, FilterCondition, Value }` or `GroupNode { Filters[] }`, both with `FilterLogicOption` |
| Base (global) search | `query.ApplyBaseFilter(searchValue, config)` or `ApplyBaseFilter(lookups, value)` | configured with `ConfigureSearch<T>(…)` |
| Combined | `query.ApplyFilters(lookups, searchValue, nodes)` | |
| Ordering | `query.ApplyOrder(orderParameters)` | `OrderParameters(string OrderBy, OrderDirection)` |
| Pagination | `query.Paginate(page, size)` (sync, core) / `query.PaginateAsync(page, size, ct)` (from `Steverzag.QueryShaper.EntityFrameworkCore`) | returns `PaginatedList<T>` (see [Pagination](#pagination)) |
| Grouping / aggregation | `query.ApplyGrouping(keys).ApplySelect(aggregates)` | `GroupByKeys`, `SelectResult` |
| Mapping | `query.ApplySelect<TIn, TOut>(mapper)` | `AddMap<TIn,TOut>(m => m.ForMember(d => d.X, "Selector"))` |

`FilterCondition` values: `Equals`, `NotEquals`, `GreaterThan(OrEqual)`, `LessThan(OrEqual)`,
`StartsWith`, `DoesNotStartWith`, `EndsWith`, `DoesNotEndWith`, `IsEmpty`, `IsNotEmpty`, `In`, `NotIn`,
`Contains`, `DoesNotContain`, `ContainsAny`, `DoesNotContainAny`, `ContainsAll`, `DoesNotContainAll`.

## Combining filters

Each `FilterNode` carries a `FilterLogicOption` (`And` / `Or`) that says how it joins **everything
before it**:

- **Strictly left to right, with no precedence.** `A`, `Or B`, `And C` means `(A or B) and C`, not
  `A or (B and C)` as in SQL or C#. The first node's connector is ignored.
- **A node is a condition or a group** (decision 011). `FilterNode` is abstract, and its only two kinds are
  `ConditionNode` (one test on a field) and `GroupNode` (parentheses). A group has no field, condition or value
  of its own, so decision 009.5 holds by construction.
- **Groups are parentheses.** A `GroupNode`'s children combine with the same rules, and its own connector joins the
  whole group to what came before. Groups nest to any depth. To get `A or (B and C)`, write
  `A`, `Or GroupNode [B, And C]`.
- **Invariants are checked when a node is built**, also through `with`. A group needs at least one child and no
  `null` children (`ArgumentException`), and it copies the list it is given. A condition needs a
  `FieldSelector` (`ArgumentNullException`).
- An empty list leaves the query unchanged; a `null` list throws `ArgumentNullException`, and a `null` node in
  the list throws `ArgumentException`.

These rules are pinned by `tests/Steverzag.QueryShaper.Tests/Filtering/FilterNodeLogicTests.cs`.

## Filter values

A `ConditionNode.Value` (a `FilterValue`) is converted to the property's type when the filter is built (decision 008). Every
rejection below happens before the query runs.

**Numbers.** Single values and lists (`In`, `NotIn`, `ContainsAny`, `ContainsAll`) follow the same rules
(decision 013):
- Any number converts to the property's type if it fits: `50L` or `50.0` for an `int`, `1` for a `double` or `decimal`.
- A fractional part, NaN, infinity or an out-of-range value for an integer property throws
  `FilterConditionAndValueNotSupportedException`. The value is never truncated or wrapped.
- A value too large for a `float` or `decimal` property throws the same exception.
- A non-number (a numeric string, `bool`, a number for a `char` property) throws `InvalidCastException`.

**Enums.** Single values and lists follow the same rules:
- Accepted: the underlying number, a member name (case-sensitive), a numeric string, or, for a `[Flags]` enum,
  comma-separated names (`"Waterproof, Cordless"`).
- An undefined value or an unknown name throws `FilterConditionAndValueNotSupportedException`. A `[Flags]` enum accepts
  any combination of declared bits, including 0.
- Other kinds of values (`bool`, `double`, `Guid`, …) throw `FilterConditionAndValueNotSupportedException`.

**A missing value** (`Value = null`, or a `FilterValue` that holds null) works only with `IsEmpty` and
`IsNotEmpty`. With `Equals`, `NotEquals` or a comparison it throws `FilterConditionAndValueNotSupportedException`,
for every property type and also on nullable properties (decision 013). To match nulls, use `IsEmpty`. A missing
value is never read as 0 or `false`. A non-nullable enum also rejects `IsEmpty`, because it has no empty state.

**Nulls in lists.** A list may contain `null` when the property is nullable (`int?`). `NotIn` counts rows whose value
is null as "not in" the list. A `null` in the list for a non-nullable property throws
`FilterConditionAndValueNotSupportedException`.

**List conditions.**
- `In`/`NotIn` and the collection conditions work on scalar types (numbers, `string`, `char`, `bool`, `Guid`, dates,
  enums, and their nullable forms). On entities or nested collections they throw `FilterNotSupportedException`.
- An empty list throws `ArgumentException`.

**Values are SQL parameters** (decision 028). A filter value, a value list and the base search text reach the
database as parameters (`@__Value_0`), not as literals. So the same filter with another value reuses the query EF
Core already compiled, and a list is one JSON parameter whatever its length. Lists of `TimeOnly`, `TimeSpan` and
`DateTimeOffset` are the exception: they're inlined, because EF Core's SQLite provider compares those JSON elements
in the wrong text format.

## What "empty" means

`IsEmpty` / `IsNotEmpty` test for "no meaningful value", not just for null (decision 008):

| Property type | `IsEmpty` matches |
|---|---|
| `string` | null or `""` |
| number (`int`, `decimal`, …) | 0 |
| nullable number (`int?`, …) | null or 0 |
| other nullable types (`Guid?`, `bool?`, dates, enums) | null |
| collection | no elements |

For non-nullable `Guid`, `bool`, date and enum properties, `IsEmpty` is not supported and throws.

## Base search

`ApplyBaseFilter(value, configuration)` applies the search configured for `T` (`ConfigureSearch<T>(…)` or
`BaseSearchConfiguration.Configure<T>(…)`). The configuration is required: inject the `IBaseSearchConfiguration`
that `AddQueryShaper` registers. There is no ambient fallback (decision 038).

- A blank value (`null`, `""`, whitespace) leaves the query unchanged, even for a type with no search configured.
- A non-blank value on a type with no search configured throws `InvalidOperationException` (decision 010), before
  the query runs. It used to return every row, which hid the missing configuration.
- A `null` configuration throws `ArgumentNullException`.

`ApplyBaseFilter(lookups, value)` builds the search from the given lookups instead and needs no configuration.

**Lookups must be strings** (decision 040). Each lookup is a selector whose type must be `string`; anything else
(`int`, `DateTime`, `Guid`, an enum, a collection such as `List<string>`) throws `ArgumentException`:
"Search lookups must be strings: 'Quantity' is int on Product." A selector that produces a string is fine
(`Description|$FN_COALESCE("")`, `Name|$FN_STR_TOLOWER`). Non-strings used to be searched through `ToString()`,
whose text depends on the provider (a date reads differently on SQLite, SQL Server and in memory), so a search
could silently match nothing.

- A configured search (`ConfigureSearch<T>`, `BaseSearchConfiguration.Configure<T>`) checks its lookups when it's
  configured, so `AddQueryShaper` fails at startup.
- Ad-hoc lookups are checked when the search is built, which is skipped for a blank value or an empty lookup list.
- A search written as an expression (`Configure<T>((x, s) => …)`) isn't checked: what it does is up to its author.

## Pagination

`Paginate(page, size)` and `PaginateAsync(page, size, ct)` run two queries: a count, then the page's rows. Order the
query first, or the pages are not deterministic. Both return a `PaginatedList<T>`: a read-only `IReadOnlyList<T>` of
the page's items (`Count` is the number on this page), plus:

| Property | Means |
|---|---|
| `PageIndex` | the requested page, 1-based |
| `PageSize` | the requested size (the last page may hold fewer items) |
| `TotalCount` | the number of rows in the whole (filtered) source |
| `TotalPages` | `ceil(TotalCount / PageSize)`; 0 for an empty source |
| `HasPreviousPage` / `HasNextPage` | `PageIndex > 1` / `PageIndex < TotalPages` |

Arguments are checked before any query runs (decision 009). A page or size below 1 throws
`ArgumentOutOfRangeException`, and a `null` source throws `ArgumentNullException`. A page after the last one is
**not** an error: it comes back empty, with the real `TotalCount` and `TotalPages`, so a client can tell where the
data ends. That includes pages so far out that the offset doesn't fit in an `int`, where the row query is skipped.

The page can't be changed after it's built (decision 012), so its items always agree with `TotalCount` and
`TotalPages`. Because it is a collection, System.Text.Json serializes it as a plain JSON array of the items, without
the paging properties. To send those to a client, wrap the page in your own response type:

```csharp
var page = await query.PaginateAsync(request.Page, request.Size, ct);
return new { items = page, page.PageIndex, page.PageSize, page.TotalCount, page.TotalPages };
```

## Grouping and aggregates

`ApplyGrouping(keys)` groups by 1 to 21 selectors (`GroupByKeys.MaxKeys`). An empty key list, more than 21 keys,
or a `null` key throws `ArgumentException` when the grouping is built, before the query runs (decision 009).
`ApplySelect(selectors)` on the grouped query then returns
one `SelectResult` per group, and its selectors resolve against the **group**:

| Selector | Means |
|---|---|
| `Key.GroupKey0`, `Key.GroupKey1`, … | the value of the 1st, 2nd, … grouping key (keys are not returned unless selected) |
| `$FN_AGGR_COUNT` | number of rows in the group |
| `Quantity\|$FN_AGGR_SUM` (also `MIN`, `MAX`, `AVG`) | aggregate of a property over the group's rows |
| `Reviews.Stars\|$FN_AGGR_AVG` | aggregate over a nested collection, flattened across the group |

Each `SelectValue.Selector` is the selector text exactly as passed in.

The selectors are parsed as `PropertySelector<IGrouping<GroupByKeys, T>>`, so a mistake is caught when the
selector is parsed, and the error points at it in the whole selector: `Key|Missing` gives "Property 'Missing' was
not found on type 'GroupByKeys'." at position 4. A property is looked up on the grouping first (`Key`) and then on
the rows (`Quantity`), so a typo in a row property names the row type: "…'Quantiy' was not found on type
'Product'." (decision 023).

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
        new ConditionNode { FieldSelector = "Phones.PhoneType", FilterCondition = FilterCondition.ContainsAll,
                Value = new List<object> { PhoneType.Home, PhoneType.Work } },
        new GroupNode
        {
            FilterLogicOption = LogicalConnector.And,
            Filters =
            [
                new ConditionNode { FieldSelector = "Phones.Id|" + SelectorDefaults.AvgAggregateFunction,
                        FilterCondition = FilterCondition.GreaterThan, Value = 100 },
                new ConditionNode { FieldSelector = "Email", FilterCondition = FilterCondition.IsEmpty,
                        FilterLogicOption = LogicalConnector.Or },
            ],
        },
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

// In an endpoint: inject what AddQueryShaper registered and pass it (decision 038)
app.MapGet("/people", (AppDb db, IMapper mapper) => db.People.ApplySelect<AppPerson, PersonDto>(mapper));
```

**How a map fills the destination.** Each public property of the destination is filled from its `ForMember`
selector if one is registered. Otherwise it's filled from the source property with the **same name** (case-sensitive,
decision 010), or left at its default. Values are converted when the types differ (e.g. `int` → `long`). Only
properties directly on the destination can be mapped (`d => d.X`, not `d => d.Inner.X`).

**Maps are checked when registered.** A conversion that doesn't exist (e.g. `int` → `string`) throws
`InvalidOperationException`. So does a nullable value into a non-nullable one (`int?` → `int`, or `int?` → `long`),
whether it comes from `ForMember` or the same-name rule (decision 010): make the destination nullable instead.
The reverse (`int` → `int?`) is fine. `MIN`, `MAX` and `AVG` are declared nullable (`Reviews.Stars|$FN_AGGR_MAX` is
`int?`, decision 014), because over an empty collection they are `NULL` in SQL. So the same check rejects mapping
them into a non-nullable destination; a nullable destination receives `null` for a row with an empty collection.

### Possible settings (not implemented)

Ideas for settings, recorded so they're considered when the configuration is redesigned (decision 038: they go on the future `IQueryShaper`). None of them
exist yet.

| Setting | Default | What it would do |
|---|---|---|
| Allow field access in selectors | off | Today selectors read only public properties, in both forms: a public field, a `ValueTuple`'s `Item1`/`Item2` or a struct's public field is "not found" (decided 2026-11-25). Turning it on would let both forms read public fields too, with the declared type taken from the field, the same way in both forms so they can't disagree again. It belongs to PropertySelectors (a global setting like `SelectorLimits`), with QueryShaper exposing it in its configuration. Keep it off by default: EF Core translates mapped properties, so a field would mostly be useful for in-memory queries, and it widens what a client can reach. |

## Decimal math on SQLite

`ROUND`, `CEILING`, `FLOOR` and `TRUNCATE` over a `decimal` build .NET's exact `decimal` overloads (decision 024).
SQL Server and PostgreSQL translate them exactly. EF's SQLite provider can't, so a query that uses one fails with
"could not be translated". On SQLite, register the fallback from `Steverzag.QueryShaper.EntityFrameworkCore`:

```csharp
options.UseSqlite(connection).UseDecimalMathAsDouble();
```

It's an EF query interceptor that rewrites those calls to `(decimal)Math.X((double)value)` before EF compiles the
query, so SQLite runs `round(CAST(Price AS REAL), 2)`. Comparisons with the result stay numeric. The rounding itself
happens in `REAL`, so on SQLite it has `double` precision (`0.5015` rounds to `0.501`). Don't register it on other
providers: there it would turn exact SQL into `float` SQL. Ordering by a `decimal` is a separate EF limitation on
SQLite, and the fallback doesn't change it.

## Status

This is a prototype. Every feature (filtering, base search, ordering, paging, grouping, select and mapping) is
covered by tests. The public API is not final. See
[ROADMAP.md](ROADMAP.md) Phases 1, 2 and 4 for the path to a library.
