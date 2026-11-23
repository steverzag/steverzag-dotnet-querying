# Decision log

Short records of architecture decisions. Add new entries at the bottom; never rewrite old ones.
Supersede them with a new entry instead.

---

### 001: PropertySelectors has zero external dependencies
**Status:** accepted
**Why:** it is the foundation library. Any dependency it takes is forced on every consumer and on DataWorker.

### 002: DataWorker depends on PropertySelectors, never the reverse
**Status:** accepted
**Why:** this keeps the selector language reusable outside of data fetching (mapping, reporting, UI metadata).

### 003: Function dispatch through an attribute-based `SelectorRegistry` (2026-07-19)
**Status:** accepted
**Context:** string → selector resolution used hardcoded switches in the implicit operators.
**Decision:** each function selector carries `[SelectorToken(...)]` and is discovered by assembly scan.
External assemblies call `RegisterAssembly`. Instantiation uses compiled factory delegates cached per closed type.
`[ModuleInitializer]` registration was evaluated and dropped.
**Not covered:** arithmetic, value literals and plain paths keep hardcoded dispatch on purpose. They work on
different axes (structural grammar, ordered fallbacks, terminal default).
**Future:** a source generator is the eventual answer for trimming/AOT.

### 004: The repository is a lab, and libraries are carved out of it
**Status:** accepted (2026-11-22)
**Decision:** `Data/`, the sandbox app and benchmarks stay here and are never packed. Library code moves to
`src/` (roadmap Phase 1).

### 005: Package naming uses the author prefix `Steverzag.` (2026-11-22)
**Status:** accepted
**Context:** the two libraries are separate products. A shared product prefix (e.g. `Refinery.Selectors` /
`Refinery.Query`) would wrongly present them as one product family.
**Decision:** use the author prefix `Steverzag.`, following the NuGet convention that package ID = assembly name = root namespace:
- `Steverzag.PropertySelectors`
- `Steverzag.QueryShaper`, with integrations as suffixes, e.g. `Steverzag.QueryShaper.EntityFrameworkCore`

**Follow-ups:**
- ~~Choose the query product name~~: **QueryShaper** (2026-11-22).
- ~~Decide the prefix casing~~: **PascalCase, `Steverzag.`** (2026-11-22).
- Check that the IDs are available on nuget.org.
- Request ID-prefix reservation once the first package is published.

**Casing:** NuGet IDs are case-insensitive, but the first publish fixes how the ID is displayed. The .NET namespace
convention is PascalCase (`Steverzag.PropertySelectors`). Lowercase `steverzag.` matches the GitHub handle but
breaks that convention, which is why PascalCase was chosen.
**Revisit:** a product may move to a standalone brand at 1.0 if it outgrows the prefix (deprecate the old ID and
point it to the new one).

### 006: EF Core lives in a separate add-on package (2026-11-22)
**Status:** accepted
**Decision:** `Steverzag.QueryShaper` depends only on `IQueryable` (plus DI abstractions). EF Core-specific code goes in
`Steverzag.QueryShaper.EntityFrameworkCore`, which today holds only `PaginateAsync`. Add-on extension methods share
the core's namespaces (e.g. `Steverzag.QueryShaper.Pagination`), so consumers need no extra `using`.
**Why:** this keeps the core usable with any LINQ provider, and keeps EF Core version churn out of the core package.
