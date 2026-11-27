# QueryShaper (`Steverzag.QueryShaper`)

**Purpose:** turn a *data request*, meaning filters, ordering, paging, grouping/aggregates and output
mapping, into one composed `IQueryable<T>` that the database executes.

**Use it when** endpoints serve grids, lists, searches or reports whose shape is chosen by the
client. It replaces hand-written `if (request.SortBy == "name") …` plumbing.

**Non-goals:**
- An ORM, or a replacement for repositories.
- Writes or commands. QueryShaper only reads.
- Defining the selector language. That belongs to [PropertySelectors](property-selectors.md).

## Two layers: client requests and trusted code

QueryShaper has two public layers (decision 041.6):

- **`IQueryShaper`** is for **client requests**. It validates its input, reports where a request is wrong, and
  applies the options and the guard rails (see [Security](#security)). Register it with `AddQueryShaper` and inject it.
- **The extension methods** (`ApplyLogicsFilter`, `ApplyBaseFilter`, `ApplyOrder`, `ApplySelect`, `Paginate`) are
  for **trusted input built in code**. They need no dependency injection, and they have no options and no guard
  rails. **Don't pass client input to them directly**: that bypasses what `IQueryShaper` checks.

`IQueryShaper` is a thin layer over the extensions, so both build the same queries.

## Client requests

A `QueryRequest` binds from JSON with `[FromBody]`, with no setup: the converters are attached to the types.

```json
{
  "search": "phone",
  "filters": [
    { "field": "Price", "condition": "GreaterThan", "value": 10 },
    { "connector": "Or", "filters": [
        { "field": "CreatedAt", "condition": "GreaterThanOrEqual", "value": "2026-11-01" },
        { "field": "Tags", "condition": "ContainsAny", "value": ["new", "sale"] } ] }
  ],
  "order": [ { "field": "CreatedAt", "direction": "Descending" } ],
  "page": { "index": 2, "size": 20 }
}
```

```csharp
services.AddQueryShaper<AppQueryShaperConfig>(o => o.MaxPageSize = 50);

app.MapPost("/products/query", async (QueryRequest request, AppDb db, IQueryShaper shaper, CancellationToken ct) =>
{
    var page = await shaper.ApplyPagedAsync<Product, ProductDto>(db.Products, request, ct);
    return new { items = page, page.PageIndex, page.PageSize, page.TotalCount, page.TotalPages };
});
```

The page is wrapped because a `PaginatedList` serializes as a plain array, without its counts (see
[Pagination](#pagination)). It's in a stable order even when the request has none: `ApplyPagedAsync` breaks ties
with `Product`'s primary key (see [Default order](#default-order)). [`samples/QueryShaperSample`](../samples/QueryShaperSample)
runs this endpoint end to end.

**The JSON.**
- A node with `filters` is a group; otherwise it's a condition, which needs `field` and `condition`. A node with
  both, or neither, is rejected. No `"type"` field is needed.
- `connector` defaults to `And`, and `direction` to `Ascending`. Every part of the request is optional; `{}` is a
  valid request.
- `condition`, `connector` and `direction` are names (case-insensitive). Numbers are rejected, so the wire format
  never depends on the order of the enum members. (A filter **value** on an enum property still accepts its
  number.)
- `value` is a string, a number, a boolean, `null`, or a list of those. A string is converted to the property's
  type when the filter is built (see [Filter values](#filter-values)).
- `search` uses the search configured for the type (`ConfigureSearch<T>`). A client can't choose which columns
  are searched.
- `page.size` can be left out (the default is used); `page.index` defaults to 1.
- A binding error is a `JsonException`, which ASP.NET turns into a 400. Inside a filter, `Path` points at the
  top-level filter (`$.filters[1]`) and the message names the place inside it
  (`… (at 'filters[0].condition' in this filter)`). An order works the same way: `$.order[1]`, with
  `… (at 'direction' in this order)`. A custom converter can't see the path above it, so this is as
  precise as System.Text.Json allows.

**`IQueryShaper`.**

| Method | Does |
|---|---|
| `Apply<T>(source, request)` | search → filters → order |
| `Apply<TIn, TOut>(source, request)` | the same, then the map |
| `ApplySearch`, `ApplyFilters`, `ApplyOrder`, `Map` | one part on its own, with the same checks |
| `ResolvePage(page)` | fills the default size; rejects an index below 1 and a size below 1 or above `MaxPageSize` |
| `ApplyForPaging(source, request, fallbackOrder)` | `Apply`, and makes sure the rows have an order; throws `InvalidOperationException` if nothing orders them (see [Default order](#default-order)) |
| `ApplyPaged` / `ApplyPagedAsync` (EF add-on) | `ApplyForPaging` (the EF add-on passes the primary key as the fallback), then the resolved page. A request with no page gets page 1 at the default size, so it never returns every row. |

Selectors name the **source** type's properties, and the map runs last.

**Errors.** Every error in a request, whether in a selector, a value, a condition or the page, is a
`QueryRequestException`, so a server maps one type to a 400. It has:
- `Path`: where the error is, relative to what the method received. `filters[1].filters[0].field` from `Apply`,
  `[1].filters[0].field` from `ApplyFilters`, `page.size` from `ApplyPaged`, `size` from `ResolvePage`.
- `Detail`: the message without the path. `InnerException` is the original error.

Each selector is first parsed against the source type, so a type mismatch is caught before anything is built, and
its position is in the whole selector. A non-blank `search` on a type with no search configured stays an
`InvalidOperationException`: that's a server bug, not a bad request.

**Options** (`QueryShaperOptions`, set through `AddQueryShaper(o => …)` or the standard options pattern):

| Option | Default |
|---|---|
| `DefaultPageSize` | 10 |
| `MaxPageSize` | 100. A larger size is rejected, not clamped. |
| `MaxFilters` | 50. Every node of `filters` counts, groups included, so the cap also bounds how deep groups nest. One more is a `QueryRequestException` at `filters`: "A request can have at most 50 filters, groups included." The count is checked before any selector is parsed, and stops at the first node over the cap. |
| `MaxListValues` | 100. The most values in one `In`, `NotIn`, `ContainsAny` or `ContainsAll` list; each list is counted on its own. One more is a `QueryRequestException` at that condition's value (`filters[1].filters[0].value`): "A list can have at most 100 values, not 101." |
| `MaxOrders` | 10. One more is a `QueryRequestException` at `order`: "A request can have at most 10 orders, not 11." Checked before any field is parsed. |
| `MaxSearchLength` | 200 characters. A longer `search`, whitespace included, is a `QueryRequestException` at `search`: "The search can be at most 200 characters long, not 201." |
| `MemberFilter` | `MemberFilter.All`. Every `field` in `filters` and `order` is parsed through it, so a selector that reaches a hidden member is a `QueryRequestException` at that field's path. Use `MemberFilter.Portable` to accept only date and time members that translate on SQLite, SQL Server and PostgreSQL, or the EF add-on's preset for your provider (below), and `Hide<T>`, `For<T>` or per-entity `MemberRules<T>` classes to hide your own members (see PropertySelectors → "Member filters"). The configured search and the mapper are server configuration and aren't filtered. |
| `UnsortableTypes` | Empty. An `order` field whose built type (nullable unwrapped) is in the set is a `QueryRequestException` at `order[i].field`: "'Price' can't be ordered: the database can't sort decimal values." Without it, the database throws when the query runs, which a server reports as a 500. Fill it from the EF add-on: `o.UnsortableTypes.UnionWith(ProviderUnsortableTypes.Sqlite)`. Filters on those types aren't affected. |

Invalid options (a page size or any `Max…` cap below 1, a default above the maximum, or a null `MemberFilter` or `UnsortableTypes`) fail when `IQueryShaper` is first resolved.

**Provider presets** (`ProviderMemberFilters` in `Steverzag.QueryShaper.EntityFrameworkCore`, decision 044) allow the
date and time members that filter correctly on one provider. Each preset is one date/time member deep, like `Portable`,
and leaves your own types alone. Combine a preset with your own rules as usual:
`ProviderMemberFilters.SqlServer.ApplyFromAssembly(…)`. `For(context)` or `For(providerName)` picks the preset from
`Database.ProviderName`, and throws `NotSupportedException` for any other provider.

| Type | `Sqlite` | `SqlServer` | `PostgreSql` |
|---|---|---|---|
| `DateTime` | Year, Month, Day, Hour, Minute, Second, Millisecond, Date, DayOfWeek, DayOfYear | Year, Month, Day, Hour, Minute, Second, Millisecond, Date, DayOfYear, TimeOfDay | Year, Month, Day, Hour, Minute, Date, DayOfWeek, DayOfYear, TimeOfDay |
| `DateTimeOffset` | none | Year, Month, Day, Hour, Minute, Second, Millisecond, Date, DayOfYear, TimeOfDay | Year, Month, Day, Hour, Minute, Date, DayOfWeek, DayOfYear, TimeOfDay, DateTime, UtcDateTime |
| `DateOnly` | Year, Month, Day, DayOfWeek, DayOfYear | Year, Month, Day, DayOfYear | Year, Month, Day, DayOfWeek, DayOfYear, DayNumber |
| `TimeOnly` | none | Hour, Minute, Second, Millisecond | Hour, Minute |
| `TimeSpan` | none | Hours, Minutes, Seconds, Milliseconds | Days, Hours, Minutes, Milliseconds, TotalDays, TotalHours, TotalSeconds, TotalMilliseconds |

Measured on EF Core 9 with SQLite, SQL Server 2022 and PostgreSQL 17. The provider tests fail if an update changes
what translates, in either direction. Things to know:
- On PostgreSQL, `Second` (and `TimeSpan.Seconds`) is out because it rounds the fractional second instead of
  truncating it.
- PostgreSQL's `timestamptz` stores only UTC, so `DateTimeOffset.Hour` there is the UTC hour. On SQL Server it's the
  hour at the stored offset.
- The SQL Server `TimeSpan` results only cover values under a day, since SQL Server stores a `TimeSpan` as `time`.
- On SQLite, a whole `DateTimeOffset` or `TimeSpan` value can only be compared with `==`: no `>`, `<` or ordering.

**Unsortable types per provider** (`ProviderUnsortableTypes`, decision 045). These are the probed types each provider can't `ORDER BY`. `For(context)` and `For(providerName)` work as for the member filters.

| `Sqlite` | `SqlServer` | `PostgreSql` |
|---|---|---|
| `decimal`, `ulong`, `DateTimeOffset`, `TimeSpan` (`NotSupportedException` from EF, with or without `UseDecimalMathAsDouble`) | none | none |

The probed types: `bool`, `byte`, `short`, `int`, `long`, `ulong`, `float`, `double`, `decimal`, `char`, `string`, `Guid`, an enum, `byte[]` and the five date and time types.

Some types sort, but not in .NET's order, so the guard doesn't block them:
- SQL Server orders `Guid` by its own `uniqueidentifier` byte order.
- PostgreSQL puts nulls last in ascending order; .NET and SQL Server put them first.

A typical setup:
```csharp
builder.Services.AddQueryShaper<AppQueryShaperConfig>(o =>
{
    o.MemberFilter = ProviderMemberFilters.Sqlite.ApplyFromAssembly(typeof(Program).Assembly);
    o.UnsortableTypes.UnionWith(ProviderUnsortableTypes.Sqlite);
});
```

## Security

A `QueryRequest` comes from a client, so treat everything in it as untrusted. A client controls which fields are
filtered and ordered, the conditions and values, the search text and the page. It doesn't control the source query,
the configured search, the maps, or the options. This section lists what QueryShaper guards against, and what is
still yours to do.

**What QueryShaper guards against.**

| Risk | Guard |
|---|---|
| SQL injection | No SQL text is built. Selectors become expression trees, and only registered functions can be called. Every client value is sent as a query parameter, never spliced into SQL (decision 028). |
| Breaking out of your own filters | `ApplyFilters` adds its own `Where`, which the database ANDs with any `Where` already on the source. An `Or` in the request can't widen a scope you applied first. |
| A selector that crashes the process | `SelectorLimits.MaxLength` (2,000 characters) and `MaxDepth` (32) are checked on every parse (PropertySelectors → "Limits"). |
| Huge or deeply nested filter trees | `MaxFilters` (50 nodes, groups included). Checked before any selector is parsed. |
| Long lists, searches and orders | `MaxListValues` (100 per list condition), `MaxSearchLength` (200 characters), `MaxOrders` (10). |
| Returning every row | `MaxPageSize` (100). `ApplyPaged` always pages, at `DefaultPageSize` when the request has no page. |
| Reading members a client shouldn't see | `MemberFilter`. Off by default: see below. |
| A 500 from an order the database can't run | `UnsortableTypes`. Empty by default: fill it from `ProviderUnsortableTypes`. |

These guards apply only through `IQueryShaper`. The extension methods have none (see
[Two layers](#two-layers-client-requests-and-trusted-code)).

**What is yours to do.**

- **Scope the source before shaping it.** Row-level access (tenant, owner, soft delete) belongs on the source:
  `shaper.ApplyPaged(db.Orders.Where(o => o.TenantId == tenant), request)`. QueryShaper has no notion of who is
  asking.
- **Hide sensitive members with a `MemberFilter`.** The default, `MemberFilter.All`, lets a client name **every
  public property reachable from the source**, through navigations too (`Customer.PasswordHash`). Leaving a
  property out of the output DTO doesn't protect it. A filter or an order reveals values without returning them:
  `PasswordHash` `StartsWith` `"a"`, then `"ab"`, and so on, recovers the value one character at a time, and the
  order of the results leaks it too. Hide such members with `Hide<T>`, `Only<T>` or `MemberRules<T>`, and set
  `QueryShaperOptions.MemberFilter`. An allow-list (`Only<T>`) is safer than a deny-list for entities that grow.
- **A hidden member looks like a missing one** (decision 047). The client gets "Property 'PasswordHash' was not
  found on type 'Account'.", the same message, position and token as for a name that doesn't exist, and a wrongly
  cased name gets no case hint pointing at a hidden member. The real reason ("… is not allowed.") is the
  `InnerException` of the `SelectorParseException`, so log the exception, but send only `Path` and `Detail` to the
  client.
- **Error messages name your model.** `QueryRequestException` messages are written to be sent to the client, and
  they name types, properties and functions (`Property 'Missing' was not found on type 'Product'.`). If your
  entity names are sensitive, return `Path` and a generic message instead of `Detail`.
- **Bound what isn't capped.** No option limits the length of a single filter value (a `Contains` string, for
  example). It is bounded only by the size of the request body, so set the server's request size limit (Kestrel's
  `MaxRequestBodySize`, 30 MB by default) to what your endpoints need.
- **Limit query cost.** A valid request can still be expensive. Examples are a string condition on an unindexed
  column, an aggregate over a collection navigation (`Orders.Total|$FN_AGGR_SUM`, a correlated subquery) or a
  function that stops the database from using an index. The member filter doesn't cover functions. Set a command
  timeout on the database (`CommandTimeout`), and add rate limiting for public endpoints.
- **Custom functions are callable by every client.** `SelectorRegistry.RegisterAssembly` registers functions
  globally, so any function you register can be used in any request. Register only functions that are safe to run
  on client input.
- **Map the error types.** `QueryRequestException` and `JsonException` (from binding) are client errors (400).
  `InvalidOperationException` (for example, a search on a type with none configured) is a server bug: let it be a
  500.

## Building blocks

| Feature | Entry point | Input model |
|---|---|---|
| Logic filters | `query.ApplyLogicsFilter(nodes)` | `FilterNode` = `ConditionNode { Field, Condition, Value }` or `GroupNode { Filters[] }`, both with `Connector` |
| Base (global) search | `query.ApplyBaseFilter(searchValue, config)` or `ApplyBaseFilter(lookups, value)` | configured with `ConfigureSearch<T>(…)` |
| Combined | `query.ApplyFilters(lookups, searchValue, nodes)` | |
| Ordering | `query.ApplyOrder(orderParameters)` | `OrderParameters(string Field, OrderDirection Direction = Ascending)` |
| Pagination | `query.Paginate(page, size)` or `Paginate(PageRequest)` (sync, core) / `query.PaginateAsync(…, ct)` (from `Steverzag.QueryShaper.EntityFrameworkCore`) | returns `PaginatedList<T>` (see [Pagination](#pagination)) |
| Grouping / aggregation | `query.ApplyGrouping(keys).ApplySelect(aggregates)` | `GroupByKeys`, `SelectResult` |
| Mapping | `query.ApplySelect<TIn, TOut>(mapper)` | `AddMap<TIn,TOut>(m => m.ForMember(d => d.X, "Selector"))` |

`FilterCondition` values: `Equals`, `NotEquals`, `GreaterThan(OrEqual)`, `LessThan(OrEqual)`,
`StartsWith`, `DoesNotStartWith`, `EndsWith`, `DoesNotEndWith`, `IsEmpty`, `IsNotEmpty`, `In`, `NotIn`,
`Contains`, `DoesNotContain`, `ContainsAny`, `DoesNotContainAny`, `ContainsAll`, `DoesNotContainAll`.

## Combining filters

Each `FilterNode` carries a `Connector` (`And` / `Or`) that says how it joins **everything
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
  `Field` (`ArgumentNullException`).
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

**Text for dates, times, `Guid` and `char`** (decision 041.4). JSON has no date type, so a string value is converted
to the property's type when the filter is built, for single values and list elements alike. Parsing is strict and
culture-invariant:

| Property | Accepted text |
|---|---|
| `DateTime` | `2026-11-27`, `2026-11-27T14:30`, `…T14:30:00`, `…T14:30:00.1234567`. With `Z` or an offset (`+02:00`), the value is converted to UTC. Without one, it's taken as written. |
| `DateTimeOffset` | the same, and `Z` or an offset is required |
| `DateOnly` | `2026-11-27` |
| `TimeOnly` | `14:30`, `14:30:00`, `14:30:00.1234567` (24-hour) |
| `TimeSpan` | `[-][d.]hh:mm:ss[.fffffff]`: `01:30:00`, `2.01:30:00` |
| `Guid` | any format `Guid.TryParse` accepts |
| `char` | exactly one character |

Anything else (`15/03/2024`, a space instead of `T`, `9:30`, `24:00`) throws
`FilterConditionAndValueNotSupportedException` naming the field and the expected format. A value typed in code
(`DateTime`, …) is used as before. `TimeSpan`, `DateTimeOffset` and `char` have no single-value filter branch yet,
so for them this only applies inside lists (`In`, `NotIn`, the collection conditions).

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

**Case is ignored by default** (decision 050). A lookup matches when it contains the text as written, or when its
lowercase form contains the text's: `Name.Contains(s) || Name.ToLower().Contains(s.ToLowerInvariant())`. Pass
`caseSensitive: true` to `ConfigureSearch<T>(lookups, …)`, `Configure<T>(lookups, …)` or `ApplyBaseFilter(lookups, value, …)`
to match only the text as written. A search written as an expression compares as its author wrote it. The database
lowercases the column, so what folds depends on the provider:

| | SQLite | SQL Server | PostgreSQL |
|---|---|---|---|
| ASCII letters (`phone` finds "Phone") | folded | folded | folded |
| Other letters (`ángel` finds "Ángel") | **not folded**: SQLite's `lower()` is ASCII-only. The text as written still matches. | folded | folded |
| `caseSensitive: true` | compares case | **still ignores case** under the default `_CI_` collation | compares case |

Measured on EF Core 9 with SQLite, SQL Server 2022 and PostgreSQL 17 (`SearchCaseProviderTests`).

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

### Default order

Pages need a stable order: without `ORDER BY`, SQL can return rows in any order, so a row can repeat or go missing
between pages. Rows that tie on the request's order (many products in one category) have the same problem. So
`IQueryShaper` gives every type an order to page by (decision 051):

1. **The request's `order`**, validated as usual,
2. **then a tie-breaker**, added last: the default configured with `ConfigureDefaultOrder<T>`, or, when none is, the
   fallback passed to `ApplyForPaging`. The EF add-on's `ApplyPagedAsync` passes the entity's primary key. An entry the
   request already orders by isn't repeated.

With no request `order`, a source the caller already ordered (`db.Products.OrderBy(p => p.Name)`) keeps its order;
otherwise the tie-breaker alone orders it. A request's `order` replaces the caller's.

```csharp
b.ConfigureDefaultOrder<Product>([new("CreatedAt", OrderDirection.Descending), new("Id")]);
```

- End a default with a unique key, such as `Id`, so no two rows tie.
- It's server configuration: the `MemberFilter` and `MaxOrders` don't apply to it, and its fields are checked at
  startup (`ArgumentException` from `AddQueryShaper`). Configuring a type again replaces its default.
- `Apply` and `ApplyOrder` use the default too, but not the fallback, and leave a query with nothing to order by
  unordered, since they don't page.

**When nothing orders the rows**, `ApplyPaged`, `ApplyPagedAsync` and `ApplyForPaging` throw `InvalidOperationException`:
"Product can't be paged in a stable order: the request has no 'order', the source isn't ordered, and no default order
is configured. Configure one with `ConfigureDefaultOrder<Product>(…)`, or order the source." It's a server error, like a
search with no configuration. It happens only with no request order, an unordered source, no default, and no key:

- the core's sync `ApplyPaged`, which can't see EF's model (006), so configure a default for each type it pages;
- `ApplyPagedAsync` on a keyless entity, a key with a shadow property, or a source projected to another type
  (`db.Orders.Select(o => o.Customer)`).

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
        new ConditionNode { Field = "Phones.PhoneType", Condition = FilterCondition.ContainsAll,
                Value = new List<object> { PhoneType.Home, PhoneType.Work } },
        new GroupNode
        {
            Connector = LogicalConnector.And,
            Filters =
            [
                new ConditionNode { Field = "Phones.Id|" + SelectorDefaults.AvgAggregateFunction,
                        Condition = FilterCondition.GreaterThan, Value = 100 },
                new ConditionNode { Field = "Email", Condition = FilterCondition.IsEmpty,
                        Connector = LogicalConnector.Or },
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
        .ConfigureSearch<AppPerson>(["Name", "Email", "Address.State"])
        .ConfigureDefaultOrder<AppPerson>([new("Name"), new("Id")]);
}

services.AddQueryShaper<AppQueryShaperConfig>();   // multiple configurations merge into one registry

// For a client request: inject IQueryShaper (see Client requests)
app.MapPost("/people/query", (QueryRequest request, AppDb db, IQueryShaper shaper) =>
{
    var page = shaper.ApplyPaged<AppPerson, PersonDto>(db.People, request);
    return new { items = page, page.PageIndex, page.PageSize, page.TotalCount, page.TotalPages };
});

// For code-built queries: inject what AddQueryShaper registered and pass it (decision 038)
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

Ideas for settings, recorded so they're considered as `QueryShaperOptions` grows (decision 041.8). None of them
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
