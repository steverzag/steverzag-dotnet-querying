# ExpDataWorkerBeta: lab for PropertySelectors and DataWorker

This repository is the **experimentation lab** for two .NET libraries that will ship separately:

- **PropertySelectors** (`ExpDataWorker.PropertySelector/`) is a small, dependency-free language that turns
  strings like `"Phones.Id|$FN_AGGR_AVG"` or `"(Amount + 2) * 3"` into SQL-translatable LINQ expressions.
  → [docs/property-selectors.md](docs/property-selectors.md)
- **DataWorker** (`ExpDataWorkerBeta/Feature/`) is a set of `IQueryable` extensions for filtering, ordering,
  pagination, aggregation and mapping, all driven by PropertySelectors.
  → [docs/data-worker.md](docs/data-worker.md)

Where things stand and what comes next: **[docs/ROADMAP.md](docs/ROADMAP.md)**.
Architecture decisions: [docs/decisions.md](docs/decisions.md).

## Layout

| Project | Role |
|---|---|
| `ExpDataWorker.PropertySelector` | PropertySelectors library (future package) |
| `ExpDataWorker.Tests` | PropertySelectors unit tests (xUnit) |
| `ExpDataWorkerBeta` | DataWorker code (`Feature/`) plus the sandbox `Program.cs` (to be split, roadmap Phase 1) |
| `Data` | Lab-only EF Core model (SQL Server, Bogus seed data) |
| `ExpDataWorker.Benchmark` | BenchmarkDotNet experiments |

## Build and test

```bash
dotnet build
dotnet test ExpDataWorker.Tests
```

This requires the .NET 9 SDK. The sandbox app expects a SQL Server instance configured in `Data/AppDbContext.cs`.
