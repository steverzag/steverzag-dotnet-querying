# Steverzag.QueryShaper

Turns a data request (filters, ordering, paging, grouping/aggregates and output mapping) into one composed
`IQueryable<T>` that the database executes. It replaces the hand-written `if (request.SortBy == "name") …`
plumbing behind grid, list, search and report endpoints. Fields are named with
[Steverzag.PropertySelectors](https://www.nuget.org/packages/Steverzag.PropertySelectors).

```json
{
  "search": "phone",
  "filters": [
    { "field": "Price", "condition": "GreaterThan", "value": 10 },
    { "field": "Tags", "condition": "ContainsAny", "value": ["new", "sale"] }
  ],
  "order": [ { "field": "CreatedAt", "direction": "Descending" } ],
  "page": { "index": 2, "size": 20 }
}
```

```csharp
public class AppQueryShaperConfig : QueryShaperConfiguration
{
    protected override void OnConfiguring(QueryShaperConfigurationBuilder b) => b
        .AddMap<Product, ProductDto>(m => m.ForMember(d => d.ReviewCount, "Reviews|$FN_AGGR_COUNT"))
        .ConfigureSearch<Product>(["Name", "Description"])
        // The order when a request has none, and the tie-breaker when it has one, so pages are stable.
        .ConfigureDefaultOrder<Product>([new("Id")]);
}

services.AddQueryShaper<AppQueryShaperConfig>(o => o.MaxPageSize = 50);

app.MapPost("/products/query", (QueryRequest request, AppDb db, IQueryShaper shaper) =>
{
    var page = shaper.ApplyPaged<Product, ProductDto>(db.Products, request);
    // A PaginatedList serializes as a plain array, so send the counts alongside it.
    return new { items = page, page.PageIndex, page.PageSize, page.TotalCount, page.TotalPages };
});
```

## Two layers

- **`IQueryShaper`** is for client requests. It validates the request, reports where it's wrong
  (`QueryRequestException`, with a path), and applies the guard rails: page size, member allow-list, and caps on
  orders, search length and list values.
- **The extension methods** (`ApplyLogicsFilter`, `ApplyBaseFilter`, `ApplyOrder`, `ApplySelect`, `Paginate`) are
  for trusted input built in code. They have no guard rails: don't pass client input to them directly.

For EF Core (async paging, provider presets, decimal math on SQLite), add
[Steverzag.QueryShaper.EntityFrameworkCore](https://www.nuget.org/packages/Steverzag.QueryShaper.EntityFrameworkCore).

## Documentation

[docs/query-shaper.md](https://github.com/steverzag/steverzag-dotnet-querying/blob/main/docs/query-shaper.md), including the
security notes.

## Status

`0.1.0-preview`: the API may still change before 1.0. The preview targets `net9.0`, which is out of support; the
targets move at 1.0.
