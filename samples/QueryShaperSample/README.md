# QueryShaper sample

A minimal ASP.NET API with one endpoint, `POST /products/query`, that takes a `QueryRequest` (search, filters, order,
page) and returns a page of products. It runs on an in-memory SQLite database seeded at startup, so it needs nothing
installed beyond the .NET 9 SDK.

```bash
dotnet run --project samples/QueryShaperSample --urls http://localhost:5237
```

Then send the requests in [QueryShaperSample.http](QueryShaperSample.http) (Visual Studio, Rider and VS Code's REST
Client run it as is), or with curl:

```bash
curl -X POST http://localhost:5237/products/query -H "Content-Type: application/json" \
  -d '{"search":"phone","order":[{"field":"CreatedAt","direction":"Descending"}],"page":{"size":3}}'
```

## What it shows

| File | |
|---|---|
| `Program.cs` | `AddQueryShaper` with SQLite's member filter and unsortable types, the endpoint (`ApplyPagedAsync`), and a response that carries the page counts. |
| `SampleQueryShaperConfig.cs` | A map from `Product` to `ProductDto`, with aggregates (`Reviews\|$FN_AGGR_COUNT`, `Reviews.Stars\|$FN_AGGR_AVG`); the search configuration; `ProductMemberRules`, which hides `InternalNotes` from requests. |
| `QueryErrors.cs` | Request errors as a 400 with the error's `path`: `QueryRequestException` from an endpoint filter, and JSON binding errors from an exception handler. |
| `Catalog/` | The EF Core model and seed data. |

Things worth knowing, all visible in the code:
- A `PaginatedList` serializes as a plain JSON array, so the endpoint wraps it in `PageResponse` to send the counts.
- Pages are in a stable order with no setup: `ApplyPagedAsync` falls back to the primary key, and breaks ties in a
  request's `order` with it. `ConfigureDefaultOrder<Product>(…)` would set a different default.
- On .NET 9, ASP.NET logs a JSON binding error as an unhandled exception even when the handler turns it into a 400.

This sample references the library projects in this repository. In your own app, reference the package:
`dotnet add package Steverzag.QueryShaper.EntityFrameworkCore --prerelease`.
