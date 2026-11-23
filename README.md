# Steverzag lab: PropertySelectors and QueryShaper

This repository is the **experimentation lab** for two separate .NET libraries:

- **Steverzag.PropertySelectors** (`src/Steverzag.PropertySelectors/`) is a small, dependency-free language that
  turns strings like `"Phones.Id|$FN_AGGR_AVG"` or `"(Amount + 2) * 3"` into SQL-translatable LINQ expressions.
  → [docs/property-selectors.md](docs/property-selectors.md)
- **Steverzag.QueryShaper** (`src/Steverzag.QueryShaper/`) is a set of `IQueryable` extensions for filtering,
  ordering, pagination, aggregation and mapping, all driven by PropertySelectors. EF Core-specific parts live in
  `Steverzag.QueryShaper.EntityFrameworkCore`.
  → [docs/query-shaper.md](docs/query-shaper.md)

Where things stand and what comes next: **[docs/ROADMAP.md](docs/ROADMAP.md)**.
Architecture decisions: [docs/decisions.md](docs/decisions.md).

## Layout

```
src/     the libraries, which will become packages
  Steverzag.PropertySelectors/
  Steverzag.QueryShaper/
  Steverzag.QueryShaper.EntityFrameworkCore/
tests/   Steverzag.PropertySelectors.Tests/ (xUnit)
lab/     never packed
  Data/        EF Core model on SQL Server, Bogus seed data
  Sandbox/     scratch console app for trying things against the database
  Benchmarks/  BenchmarkDotNet experiments
docs/
```

## Build and test

```bash
dotnet build Steverzag.Lab.sln
dotnet test Steverzag.Lab.sln
```

This requires the .NET 9 SDK. The sandbox app expects a SQL Server instance configured in `lab/Data/AppDbContext.cs`.
