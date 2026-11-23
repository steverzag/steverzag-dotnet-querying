# Roadmap

> Last reviewed: 2026-11-22. This repository is the **lab** for two products that will each ship
> as their own library. This file explains where things stand and the order of work to get there.

## 1. The two products

| | **PropertySelectors** | **QueryShaper** |
|---|---|---|
| **What it is** | A small language that turns a string into a LINQ `Expression`. For example, `"Phones.Id\|$FN_AGGR_AVG"` becomes `p => p.Phones.Select(x => x.Id).Average()`. | A set of `IQueryable<T>` extensions that run **filter, order, paginate, group/aggregate and map** requests described as data. It uses PropertySelectors to name fields. |
| **Problem it solves** | A client (HTTP API, saved report, UI grid) needs to point at a property, a computed value or an aggregate **without compiled lambdas**, and the result must still translate to SQL. | It removes the hand-written query plumbing behind "grid" and "report" endpoints: one request DTO is turned into one translatable query. |
| **Audience** | Library authors and anyone who builds dynamic expressions. | Application developers writing data-access APIs (mainly EF Core). |
| **Dependencies** | **None** (BCL only). This must stay true. | Core: PropertySelectors + `Microsoft.Extensions.DependencyInjection.Abstractions`. `.EntityFrameworkCore` add-on: EF Core (async pagination). |
| **Project** | `src/Steverzag.PropertySelectors/` | `src/Steverzag.QueryShaper/` + `src/Steverzag.QueryShaper.EntityFrameworkCore/` |
| **Tests** | `tests/Steverzag.PropertySelectors.Tests/` (367 tests, all passing) | **None yet** |

**The dependency rule:** QueryShaper depends on PropertySelectors, never the other way round.
PropertySelectors knows nothing about filtering, EF Core or DI.

More detail is in [property-selectors.md](property-selectors.md) and [query-shaper.md](query-shaper.md).

## 2. Where we are (snapshot)

### PropertySelectors: about 75% of the way to a first preview
- ✅ Property paths, nested navigation, and collection projection (`Phones.Number`).
- ✅ Function selectors, each with a generic and a non-generic version:
  - aggregation (Count, LongCount, Sum, Avg, Min, Max)
  - element (First, Last, ElementAt)
  - ordering (OrderBy/ThenBy, ascending and descending)
  - math (19 functions)
  - string (17 functions)
  - date (10 functions)
- ✅ Literal values (`42`, `true`, `"text"`, `$VAL_*:` specifiers) and an arithmetic grammar (`(Amount + 2) * 3`).
- ✅ `SelectorRegistry`: attribute-based (`[SelectorToken]`), open to external assemblies, with cached compiled factories.
- ✅ `PropertiesExplorer<T>` / `SelectorsMetaDataTree`, which expose metadata for UIs.
- ⚠️ Known gaps:
  - Non-generic arithmetic on properties throws (`(PropertySelector)"Age * 2"`, see `PropertySelectorUtils.cs:31`).
  - Avg always returns `double` (`AvgAggregationSelector.cs:8`).
  - Overload resolution goes by argument **count**, not type (`FunctionPropertySelector.cs:31`).
  - `$FN_AGGR_COUNTDISTINCT` is declared but has no selector.
  - `SyntheticFunction` is an empty stub.
  - Generic and non-generic record hierarchies are duplicated.
  - Public API surface (namespace, `public` vs `internal`) has not been curated.

### QueryShaper: about 50%, still a prototype
- ✅ Filtering: a `FilterNode` tree with And/Or logic and 20 `FilterCondition`s, plus a configurable base (global) search.
- ✅ Ordering (`ApplyOrder`), pagination (`Paginate` in core, `PaginateAsync` in the EF Core add-on).
- ✅ Grouping and aggregation (`ApplyGrouping` / `ApplySelect` → `SelectResult`).
- ✅ Configuration: `QueryShaperConfiguration` + `AddQueryShaper<T>()`, a selector-based `Mapper`, and `BaseSearchConfiguration`.
- ⚠️ Known gaps:
  - **No tests.**
  - Stray auto-imported usings showed up twice (Roslyn, `Microsoft.Diagnostics.Symbols`). They are gone now that the library no longer sees the lab packages transitively.
  - Two `NotImplementedException` branches in `FilteringExtensions.cs` (~L520, ~L577) and a TODO about Contains semantics (~L466).
  - `ValueResult2` / `SearchValue.cs` naming is temporary.
  - `PaginationParameters` is internal and unused.
  - The `QueryShaperServices` static ambient registry needs a decision (see §5).
  - No request DTO that an HTTP endpoint could bind to in one go.

### Lab infrastructure
- `lab/Data/`: an EF Core model (`AppPerson` and related entities) on SQL Server with migrations, used as a realistic playground.
- `lab/Sandbox/`: a scratch console app. `lab/Benchmarks/`: BenchmarkDotNet experiments. Lab projects keep their old `ExpDataWorker.*` namespaces on purpose, so the EF migrations stay untouched.
- Shared `Directory.Build.props` and GitHub Actions CI were added in Phase 0. There is no package metadata yet.

## 3. Where we are heading

The end state is two NuGet packages, both versioned independently but released from this repository:

```
Steverzag.PropertySelectors                    ← zero dependencies, AOT/trim-friendly in the long run
Steverzag.QueryShaper                       ← depends on PropertySelectors + DI abstractions
Steverzag.QueryShaper.EntityFrameworkCore   ← async pagination / EF-specific bits
```
These are separate products under a shared author prefix (see decision 005).

The lab (`lab/`: Data, Sandbox, Benchmarks) stays in the repository and is **never packed**.

## 4. Phases

Each phase ends in a state where everything compiles, all tests are green, and the docs are current.

### Phase 0: Restore and stabilize the lab ✅ (2026-11-22)
- [x] Fix the Benchmark build (the missing `using`).
- [x] Remove the stray `Microsoft.CodeAnalysis` usings (`OrderParameters.cs`, `AppDbContext.cs`) and the unused Roslyn package from `Data`.
- [x] Add `Directory.Build.props` with shared `TargetFramework`, `LangVersion`, `ImplicitUsings`, `Nullable` and `Deterministic`. `TreatWarningsAsErrors` is opt-in per project.
- [x] Triage warnings. All 73 were nullable-reference warnings. The 21 in PropertySelectors are fixed, and that library and its tests now build with `TreatWarningsAsErrors`. The remaining 51 are in QueryShaper (40) and Data (11): mostly `GroupByKeys.cs` (21) and `FilteringExtensions.cs` (19). They are handled in Phase 2.
- [x] Add a CI workflow (`.github/workflows/ci.yml`) that runs build and test on pushes to `main` and on pull requests.
- [x] Docs initialized (this file, the product docs, the decision log).

### Phase 1: Separate library code from lab code ✅ (2026-11-22)
- [x] Move the former `ExpDataWorkerBeta/Feature/**` code into the class library `src/Steverzag.QueryShaper/`. Its only package dependency is DI abstractions.
- [x] Move the sandbox app to `lab/Sandbox/`. It keeps Bogus, BenchmarkDotNet and SqlServer.
- [x] Adopt this folder layout, with a new `Steverzag.Lab.sln` organized into `src`/`tests`/`lab` solution folders:
  ```
  src/Steverzag.PropertySelectors/   src/Steverzag.QueryShaper/   src/Steverzag.QueryShaper.EntityFrameworkCore/
  tests/Steverzag.PropertySelectors.Tests/
  lab/Data/   lab/Sandbox/   lab/Benchmarks/
  docs/
  ```
- [x] Rename namespaces: `ExpDataWorker.PropertySelectors` → `Steverzag.PropertySelectors` and `ExpDataWorkerBeta.Feature.*` → `Steverzag.QueryShaper.*`. Rename the configuration types: `AddDataWorker` → `AddQueryShaper`, and `DataWorker*` → `QueryShaper*`.
- [x] Decide on EF Core coupling (006). `PaginateAsync` moved to `Steverzag.QueryShaper.EntityFrameworkCore` and gained a `CancellationToken`. The core gained a sync `Paginate`.

### Phase 2: QueryShaper test baseline
- [ ] Create `tests/Steverzag.QueryShaper.Tests` that runs against **EF Core InMemory or SQLite**, so SQL translation is exercised rather than just LINQ-to-objects.
- [ ] Cover each `FilterCondition`, nested `FilterNode` logic, ordering, pagination edges, grouping/aggregation, the mapper, and the base search.
- [ ] Resolve the `NotImplementedException` branches and the Contains-semantics TODO.
- [ ] Clear the remaining nullable warnings in QueryShaper and Data, then enable `TreatWarningsAsErrors` there.

### Phase 3: Close the PropertySelectors gaps
- [ ] Non-generic arithmetic with properties (`PropertySelectorUtils.cs:31`).
- [ ] Avg return type follows the element type (decimal/float).
- [ ] Type-aware overload resolution.
- [ ] Implement `$FN_AGGR_COUNTDISTINCT`, and either finish or delete `SyntheticFunction`.
- [ ] Clear error messages that give the position and token for invalid selectors (a `SelectorParseException`).
- [ ] (Optional) Unify the duplicated generic/non-generic record hierarchies.

### Phase 4: Shape the public API (the gate before any package ships)
- [x] Choose the product names and root namespaces, and rename the code (done in Phase 1).
- [ ] Audit `public` vs `internal`, and add XML docs to everything public.
- [ ] Rename temporaries (`ValueResult2`, `SearchValue.cs`, the `SyntheticFuncions` typo folder).
- [ ] Freeze the selector **grammar** and write it down as a spec (`docs/property-selectors.md` → "Grammar").
- [ ] Add a QueryShaper request contract: a single `QueryRequest { Filters, Order, Page, Search }` that binds from JSON, plus an `Apply(request)` entry point.
- [ ] Add a security note and guard rails: a property allow-list, max depth, and a page-size cap, because selectors arrive from untrusted clients.

### Phase 5: Package and release `0.1.0-preview`
- [ ] Package metadata (`PackageId`, description, license, README in the package, SourceLink, deterministic builds).
- [ ] Add `CHANGELOG.md` per package, use SemVer, and tag the repository per package (`propertyselectors-v0.1.0`).
- [ ] Publish PropertySelectors first, since it has no dependencies. Publish QueryShaper against the released PropertySelectors version.
- [ ] Add a samples project: a minimal ASP.NET endpoint that uses QueryShaper end to end.

### Phase 6: Later
- Source generator for selector discovery (trimming/AOT).
- Additional providers (beyond EF Core), and a projection/`Select` DSL.
- Benchmarks published in the docs, and an optional docs site.
- Possibly split into separate repositories once the APIs are stable.

## 5. Open decisions (owner: you)

| # | Decision | Recommendation |
|---|---|---|
| D1 | Product/package names and namespace root | ✅ **Decided (005):** `Steverzag.PropertySelectors`, `Steverzag.QueryShaper`, `Steverzag.QueryShaper.EntityFrameworkCore`. |
| D2 | Target framework(s) | `net8.0` (LTS) + `net9.0`. That widens adoption, and the code uses nothing that needs 9 except possibly `SearchValues`, which exists in 8. |
| D3 | EF Core coupling | ✅ **Decided (006):** the core uses only `IQueryable`. EF Core-specific code lives in `Steverzag.QueryShaper.EntityFrameworkCore`. |
| D4 | `QueryShaperServices` static ambient registry | Keep it only as a fallback. Make the explicit `IMapper`/`IBaseSearchConfiguration` parameters the primary path, because static state breaks tests and multi-tenant setups. |
| D5 | Should `$VAL_*` specifiers start with `$SPEC_`? | `SpecifierStart` exists but nothing uses it. Remove it, or document `$VAL_` as canonical. |
| D6 | Monorepo vs. two repositories | Stay a monorepo until 1.0. |

Recorded decisions live in [decisions.md](decisions.md).
