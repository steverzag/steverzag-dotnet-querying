# Steverzag.QueryShaper.EntityFrameworkCore

The EF Core add-on for [Steverzag.QueryShaper](https://www.nuget.org/packages/Steverzag.QueryShaper). The core
works on any `IQueryable`; this package adds what needs EF Core.

- **Async paging:** `shaper.ApplyPagedAsync<TIn, TOut>(source, request, ct)` for client requests, and
  `PaginateAsync` for queries built in code.
  ```csharp
  app.MapPost("/products/query", (QueryRequest request, AppDb db, IQueryShaper shaper, CancellationToken ct)
      => shaper.ApplyPagedAsync<Product, ProductDto>(db.Products, request, ct));
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
