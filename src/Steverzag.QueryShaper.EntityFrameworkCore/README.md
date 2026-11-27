# Steverzag.QueryShaper.EntityFrameworkCore

The EF Core add-on for [Steverzag.QueryShaper](https://www.nuget.org/packages/Steverzag.QueryShaper). The core
works on any `IQueryable`; this package adds what needs EF Core.

- **Async paging:** `shaper.ApplyPagedAsync<TIn, TOut>(source, request, ct)` for client requests, and
  `PaginateAsync` for queries built in code. When no default order is configured, `ApplyPagedAsync` breaks ties with
  the entity's primary key, so pages are stable with no setup.
  ```csharp
  app.MapPost("/products/query", async (QueryRequest request, AppDb db, IQueryShaper shaper, CancellationToken ct) =>
  {
      var page = await shaper.ApplyPagedAsync<Product, ProductDto>(db.Products, request, ct);
      // A PaginatedList serializes as a plain array, so send the counts alongside it.
      return new { items = page, page.PageIndex, page.PageSize, page.TotalCount, page.TotalPages };
  });
  ```
- **Provider presets:** `ProviderMemberFilters.For(dbContext)` and `ProviderUnsortableTypes.For(dbContext)` give
  the members and types that translate on SQLite, SQL Server and PostgreSQL, for `QueryShaperOptions`.
- **Decimal math on SQLite:** `options.UseSqlite(connection).UseDecimalMathAsDouble()` lets `ROUND`, `CEILING`,
  `FLOOR` and `TRUNCATE` over a `decimal` translate on SQLite, in `double` precision. Register it on SQLite only.

## Documentation

[docs/query-shaper.md](https://github.com/steverzag/steverzag-dotnet-querying/blob/main/docs/query-shaper.md).

## Status

`0.1.0-preview`, built against EF Core 9. The API may still change before 1.0, when the add-on's major starts
following EF Core's.
