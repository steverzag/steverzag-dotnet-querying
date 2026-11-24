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

### 007: Filter nodes combine strictly left to right, with no precedence (2026-11-22)
**Status:** accepted
**Context:** each `FilterNode` carries its own `And`/`Or` connector. `A`, `Or B`, `And C` can be read
left to right as `(A or B) and C`, or with SQL/C# precedence (AND before OR) as `A or (B and C)`.
**Decision:** keep left to right, which is how the code was built. Each node's connector joins it to everything
before it, and the first node's connector is ignored. A node with child `Filters` is a group (parentheses), and
groups are how a client overrides the order. The rules are documented in `query-shaper.md` → "Combining filters" and
pinned by `FilterNodeLogicTests`.
**Considered:** AND-before-OR precedence, which matches what readers of the JSON expect from SQL/C#. It was rejected
in favour of the as-built "running total" model, which maps directly onto a row-by-row filter-builder UI.
**Consequence:** the docs must state the rule clearly, because it differs from SQL.
**Follow-up (2026-11-22):** when QueryShaper gets its configuration approach, add a switch between this behaviour and
AND-before-OR precedence. Proposed name: `FilterEvaluationOrder { LeftToRight, AndBeforeOr }`, defaulting to `LeftToRight`.
Tracked in ROADMAP Phase 4.

### 008: How filter values are interpreted (2026-11-22)
**Status:** accepted; implemented (2026-11-23)
**Context:** the Phase 2 tests pinned several rules that were never chosen on purpose, and some disagree
with each other: single values accept any integer size but lists don't, and enum names work in lists but not
in `Equals`.
**Decision:**
1. **`IsEmpty` on a nullable number matches null or 0. Kept as is.** It follows the same rule as strings
   (null or `""`) and non-nullable numbers (0): "empty" means "no meaningful value", not just "null".
2. **Undefined enum values are rejected** with `FilterConditionAndValueNotSupportedException`
   (e.g. `Status = 99`). Exception: enums marked `[Flags]`, where combinations of defined values are valid.
3. **List values are converted safely to the target type.** `In`, `ContainsAny` and `ContainsAll` accept any
   numeric value that fits (e.g. a `long` for an `int` property), with overflow checks. Values that don't fit, or
   aren't numeric, still throw. This matches single values, which already accept any integer size. Converting
   JSON-bound values (`JsonElement`) belongs to the Phase 4 request contract.
4. **Enum names are accepted everywhere** (`Equals`/`NotEquals` as well as lists), in addition to underlying numbers.
**Cleanups that come with it:**
- Bad enum or null input throws `FilterConditionAndValueNotSupportedException`, not a plain `System.Exception`.
- `ApplyLogicsFilter` no longer writes every expression to `Console`.

### 009: Invalid requests are rejected up front, not silently reinterpreted (2026-11-22)
**Status:** accepted; implementation pending (ROADMAP Phase 2)
**Context:** the tests found inputs that are silently ignored, produce nonsense, or crash deep inside EF Core.
Requests come from untrusted clients (see the Phase 4 guard rails), so a clear error is safer than a guess.
**Decision:**
5. **A group node with its own condition is rejected.** If a `FilterNode` has child `Filters`, its
   `FieldSelector`/`FilterCondition`/`SearchValue` must be unset. Today they are silently ignored (see 007).
6. **Grouping with no keys is rejected** with `ArgumentException`. Today EF Core crashes while translating it.
   "Aggregate the whole table" may be added later as an explicit feature.
7. **Pagination arguments are validated.** Page < 1 or size < 1 throws `ArgumentOutOfRangeException`. Today
   page 0, size 0 and size −1 return nonsense, and size −1 returns every row on SQLite. `PaginatedList`
   also gains `TotalCount` and `PageSize`; adding them breaks nothing.
**Cleanup that comes with it:** more than 21 grouping keys throws a descriptive `ArgumentException`, not a bare
`ArgumentNullException` from `Expression.Bind`.

### 010: Mapper and base search fail at configuration time, not silently at query time (2026-11-22)
**Status:** accepted; implementation pending for 8 and 9 (ROADMAP Phase 2)
**Decision:**
8. **A non-blank base search on a type with no configuration throws** `InvalidOperationException`, the same as
   `GetSearchPredicate` already does. Today the query is returned unfiltered, which hides a misconfiguration.
   A blank search still leaves the query unchanged.
9. **Mapping a nullable source into a non-nullable destination is rejected when the map is registered**
   (e.g. `int?` → `int`). The error suggests making the destination nullable. Today the map registers fine and
   then fails on the first row with a null.
10. **The same-name rule stays case-sensitive. Kept as is.** These are C# property names, and ignoring case could
    match two properties.

### 011: A filter node is either a condition or a group (2026-11-23)
**Status:** accepted; implemented (2026-11-23)
**Context:** `FilterNode` carried a condition (`FieldSelector`, `FilterCondition`, `SearchValue`) and child `Filters`
on one type. Decision 009.5 then needed a runtime check that a group sets no condition, and that check couldn't see
`FilterCondition.Equals`, the enum's default. `FieldSelector` was `= null!`, so a condition without a field failed
deep inside `CreateMemberAccess`. A node with an empty `Filters` list silently became a plain filter.
**Decision:** `FilterNode` is an abstract record with exactly two kinds: `ConditionNode` (required `FieldSelector`,
plus `FilterCondition` and `SearchValue`) and `GroupNode` (required, non-empty `Filters` with no nulls, copied).
`FilterLogicOption` stays on the base. The constructor is `private protected`, so no other assembly can add a node
kind (C# has no closed unions, so a `switch` over nodes still needs a default arm). The property names are unchanged;
renaming them is part of the Phase 4 API audit.
**Considered:** keeping one type and making `FilterCondition` nullable, which closes only the `Equals` gap.
**Consequence:** a breaking change, made while nothing is released. System.Text.Json can't bind `FilterNode` yet.
That was true before the split as well (`ValueResult2` and `PropertySelector` have no converters). The Phase 4
request contract adds a converter that tells the two kinds apart by shape: a node with `filters` is a group,
anything else is a condition, and a node with both is rejected. The JSON needs no `"type"` discriminator.
