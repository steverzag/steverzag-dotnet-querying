# Roadmap

> Last reviewed: 2026-11-24. This repository is the **lab** for two products that will each ship
> as their own library. This file explains where things stand and the order of work to get there.

## 1. The two products

| | **PropertySelectors** | **QueryShaper** |
|---|---|---|
| **What it is** | A small language that turns a string into a LINQ `Expression`. For example, `"Phones.Id\|$FN_AGGR_AVG"` becomes `p => p.Phones.Select(x => x.Id).Average()`. | A set of `IQueryable<T>` extensions that run **filter, order, paginate, group/aggregate and map** requests described as data. It uses PropertySelectors to name fields. |
| **Problem it solves** | A client (HTTP API, saved report, UI grid) needs to point at a property, a computed value or an aggregate **without compiled lambdas**, and the result must still translate to SQL. | It removes the hand-written query plumbing behind "grid" and "report" endpoints: one request DTO is turned into one translatable query. |
| **Audience** | Library authors and anyone who builds dynamic expressions. | Application developers writing data-access APIs (mainly EF Core). |
| **Dependencies** | **None** (BCL only). This must stay true. | Core: PropertySelectors + `Microsoft.Extensions.DependencyInjection.Abstractions`. `.EntityFrameworkCore` add-on: EF Core (async pagination). |
| **Project** | `src/Steverzag.PropertySelectors/` | `src/Steverzag.QueryShaper/` + `src/Steverzag.QueryShaper.EntityFrameworkCore/` |
| **Tests** | `tests/Steverzag.PropertySelectors.Tests/` (500 tests, all passing) | `tests/Steverzag.QueryShaper.Tests/` on SQLite (520 tests covering every feature; 1 skipped as a known gap) |

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
  - Overload resolution goes by argument **count**, not type (`FunctionPropertySelector.cs:31`).
  - `$FN_AGGR_COUNTDISTINCT` is declared but has no selector.
  - `SyntheticFunction` is an empty stub.
  - Generic and non-generic record hierarchies are duplicated.
  - No consistent rule for whether a function takes its subject from the pipe or from an argument (D7, decision 016).
  - Function arguments have no type when they're parsed (only literals do).
  - Public API surface (namespace, `public` vs `internal`) has not been curated.

### QueryShaper: about 50%, still a prototype
- ✅ Filtering: a `FilterNode` tree with And/Or logic and 20 `FilterCondition`s, plus a configurable base (global) search.
- ✅ Ordering (`ApplyOrder`), pagination (`Paginate` in core, `PaginateAsync` in the EF Core add-on).
- ✅ Grouping and aggregation (`ApplyGrouping` / `ApplySelect` → `SelectResult`).
- ✅ Configuration: `QueryShaperConfiguration` + `AddQueryShaper<T>()`, a selector-based `Mapper`, and `BaseSearchConfiguration`.
- ⚠️ Known gaps:
  - **Every feature now has tests** (Phase 2). They found 6 bugs and 2 validation gaps, all fixed.
  - Stray auto-imported usings showed up twice (Roslyn, `Microsoft.Diagnostics.Symbols`). They are gone now that the library no longer sees the lab packages transitively.
  - ~~Two `NotImplementedException` branches and the Contains TODO~~: resolved in Phase 2.
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

### Phase 2: QueryShaper test baseline ✅ (2026-11-24)
- [x] Create `tests/Steverzag.QueryShaper.Tests` on **SQLite in-memory** (not EF InMemory, which doesn't translate to SQL). It uses its own test model (`Infrastructure/`), and the seed table is documented in `SeedData.cs`.
- [x] Cover each `FilterCondition` ✅ (204 tests), nested `FilterNode` logic ✅ (20 tests), ordering ✅ (26 tests), pagination edges ✅ (18 tests), grouping/aggregation ✅ (19 tests), the mapper ✅ (13 tests), and the base search with configuration/DI ✅ (27 tests).
- [x] Resolve the `NotImplementedException` branches and the Contains-semantics TODO (2026-11-22). `In`/`NotIn`, `ContainsAny` and `ContainsAll` now share one value-list helper (`CreateTypedValueList`). It supports every scalar type and its nullable form (e.g. `char`, `List<int?>`), and allows `null` in the list for nullable targets. It throws `FilterNotSupportedException` for non-scalar targets (entities, nested collections). `ContainsAny` on enum collections also works now. The ContainsAny TODO was settled by the earlier fix.
- [x] Fix the bugs the FilterCondition tests found (2026-11-22). Each fix is covered by tests that fail on the old code:
  - `ContainsAny`/`DoesNotContainAny` always threw: the guard in `CreateContainsAnyExpression` was inverted (`Count > 0`).
  - Comparisons on nullable `DateTime`/`DateOnly`/`TimeOnly` threw, because the constant was not typed nullable.
  - `In`/`NotIn` on a nullable property threw (`List<int>` vs `int?`). Null rows now count as "not in" for `NotIn`.
- [x] Decide on the filter-value semantics the tests pinned down (2026-11-22, **decision 008**). Keep `IsEmpty` = null or 0 on nullable numbers, reject undefined enum values (except `[Flags]`), convert list values safely, and accept enum names everywhere.
- [x] Fix the ordering bug the tests found (2026-11-22): the list overload of `ApplyOrder` dropped everything after the first `|` segment, so `"Reviews|$FN_AGGR_COUNT"` ordered by `"Reviews"` and EF failed to translate it. It went through `OrderParameters<T>`, whose base string is only `PlainValue`. `ApplyOrder` now uses the typed selector, or parses the original string.
- [x] Fix the grouping/select bugs the tests found (2026-11-22). **Grouped `ApplySelect` could not run at all:** it parsed selectors as `PropertySelector<T>` (against the element), so `"Key.GroupKey0"` and `"$FN_AGGR_COUNT"` failed. It now uses the non-generic `PropertySelector`, which resolves against the group (`PropertySelector<IGrouping<K,T>>` can't, see Phase 3). **Both `ApplySelect` overloads labelled values with `PlainValue`** (the first segment), so `"Quantity|$FN_AGGR_SUM"` and `"Quantity|$FN_AGGR_AVG"` both came back as `"Quantity"`. Labels are now the caller's selector text.
- [x] Decide what grouping with **no keys** means (2026-11-22, **decision 009**): reject it with `ArgumentException`. "Aggregate the whole table" may come later as an explicit feature. Still open, for Phase 4: `SelectResult` doesn't include the group keys unless `Key.GroupKeyN` is selected, and `GroupedByProperty` / `AggregateParameters` (incl. `NTop`) are unused.
- [x] Decide on pagination validation (2026-11-22, **decision 009**): page < 1 or size < 1 throws `ArgumentOutOfRangeException`, and `PaginatedList` gains `TotalCount` and `PageSize`.
- [x] And/Or evaluation order: strictly left to right, no AND-before-OR precedence (decision 007, documented in [query-shaper.md](query-shaper.md#combining-filters)).
- [x] Decide whether a group node that also has its own condition should be rejected (2026-11-22, **decision 009**): yes, with a validation error.
- [x] Fix the validation gaps the mapper/base-search tests found (2026-11-22). `ForMember` accepted any property access, so `d => d.Details.Category` or `d => d.Name.Length` were accepted and then silently never applied; it now requires a property directly on the destination. `Configure<T>([])` (no lookups) crashed with `ArgumentNullException`; it now throws a descriptive `ArgumentException`.
- [x] Decide on the mapper/search behaviour the tests pinned down (2026-11-22, **decision 010**). A non-blank search on an unconfigured type throws, and `int?` → `int` maps are rejected when registered. The same-name rule stays case-sensitive.
- [x] **Implement decisions 008–010** (done 2026-11-23). The existing tests pin the old behaviour, so each change updates its tests and adds any that are missing:
  - [x] 008.2: reject undefined enum values (allow `[Flags]` combinations) (2026-11-23). Applies to single values and lists (`In`/`NotIn`, `ContainsAny`, `ContainsAll`) and throws `FilterConditionAndValueNotSupportedException`. A `[Flags]` enum accepts any combination of declared bits, including 0. The test model gained `Product.Features` (`[Flags] ProductFeatures`).
  - [x] 008.3: convert list values safely to the target type, with overflow checks (in `CreateTypedValueList`) (2026-11-23). Any numeric value converts to a numeric target if it fits (`ConvertNumber`). Integer targets reject fractional parts, NaN and out-of-range values instead of truncating or wrapping. Values that don't fit throw `FilterConditionAndValueNotSupportedException`; non-numeric values (strings, `bool`, numbers for a `char` property) still throw `InvalidCastException`.
  - [x] Single-value filters narrowed **unchecked** (`ValueResult2`'s explicit operators used plain `(int)`, `(short)`, `(byte)` casts), so `Quantity = 4294967346L` was read as `50`. Found while implementing 008.3 and fixed to match it (2026-11-23). The casts are now `checked`, and double → float checks for overflow to infinity. `BuildFilterExpression` turns the `OverflowException` into `FilterConditionAndValueNotSupportedException`. The enum branch reads the value as `long`, so out-of-range values are rejected by `Enum.TryParse` instead of wrapping to a defined member.
  - [x] 008.4: accept enum names in `Equals`/`NotEquals` (2026-11-23). Single values and lists now share one parser (`ParseEnumValue`). It accepts the underlying number, a member name (case-sensitive), a numeric string, or comma-separated names for a `[Flags]` enum. Unknown names throw `FilterConditionAndValueNotSupportedException`, the same as undefined numbers.
  - [x] 008 cleanups (2026-11-23). The enum branch's two plain `System.Exception`s now throw `FilterConditionAndValueNotSupportedException` with descriptive messages: a missing value on a non-nullable enum (which also covers `IsEmpty`/`IsNotEmpty` there), and an unsupported value kind (`bool`, `double`, `Guid`, …). `ApplyLogicsFilter` no longer writes every expression to `Console`. QueryShaper now has no plain `System.Exception` throws left. The four in PropertySelectors' arithmetic parser belong to Phase 3's `SelectorParseException`.
  - [x] 009.5: reject a group node that has its own condition (2026-11-23). It was first a runtime check, which couldn't detect `FilterCondition.Equals` (the enum's default). It was then replaced by **decision 011**: `FilterNode` is split into `ConditionNode` and `GroupNode`, so a group can't carry a condition at all. Also checked when a node is built: a group needs at least one child and no nulls, and a condition needs a field. A group with an empty `Filters` list no longer silently acts as a plain filter. The combining loop is shared by the top-level list and groups (`CombineNodes`).
  - [x] 009.6: reject `ApplyGrouping([])`; give more than 21 keys a descriptive error (2026-11-23). Both throw `ArgumentException` (param `groupByParameters`) before any expression is built. A `null` list throws `ArgumentNullException`, and a `null` key `ArgumentException`. The limit is now `GroupByKeys.MaxKeys`, with a test that keeps it in sync with the `GroupKeyN` slots.
  - [x] 009.7: validate `Paginate`/`PaginateAsync` arguments; add `PaginatedList.TotalCount` and `PageSize` (2026-11-23). Page or size < 1 throws `ArgumentOutOfRangeException` before any query runs; a `null` source throws `ArgumentNullException`. The public `PaginatedList` constructor checks the same, plus `count >= 0`. The sync and async paths share an internal `PaginationGuard` (the core now has `InternalsVisibleTo` for the EF add-on). Also fixed: the offset `(page - 1) * size` overflowed `int` for far pages (e.g. `Paginate(int.MaxValue, 2)`) and wrapped to a negative `Skip`, which SQLite reads as 0, so those pages returned the first rows. It is now computed as `long`, and a page beyond `int.MaxValue` rows is empty without running the row query.
  - [x] `PaginatedList<T>` is read-only (2026-11-23, **decision 012**). It was a `List<T>` subclass, so `Add`/`Remove`/`Clear` could make the items disagree with `TotalCount`/`TotalPages`. It is now a `sealed` `IReadOnlyList<T>` over a private copy of the items, with get-only properties. The constructor takes any `IEnumerable<T>`. Every caller only read the page, so no call site changed.
  - [x] 010.8: throw for a non-blank base search on an unconfigured type (2026-11-23). `ApplyBaseFilter(value, configuration)` throws `InvalidOperationException` naming the type and how to configure it. A blank value still returns the query unchanged, also for an unconfigured type. Unchanged: with no `IBaseSearchConfiguration` at all, even a blank search throws.
  - [x] 010.9: reject nullable → non-nullable mappings at registration (2026-11-23). `Mapper.Register` throws `InvalidOperationException` when a nullable value type maps into a non-nullable value type, by `ForMember` or by name, also when the conversion widens (`int?` → `long`). The message names the destination property and suggests making it nullable. The check sees only the selector's declared type. `MIN`/`MAX`/`AVG` used to be declared non-nullable and slipped through; since decision 014 (Phase 3) they are nullable and rejected here too.
  - [x] Update `query-shaper.md` with the resulting rules (2026-11-23). The 009/010 rules went in with each change. A new "Filter values" section covers 008: number conversion, enums, nulls and list conditions. Writing it down turned up the two inconsistencies below.
- [x] **Decided and implemented (2026-11-23, decision 013.1):** single values now convert with the same `ConvertNumber` as lists. The seven numeric branches in `FilteringExtensions` became one. The original finding: **single-value numbers are stricter than lists** (found 2026-11-23). A single value must be the same kind of number as the property, so `Score > 1` on a `double` property and `Price > 1` on a `decimal` one throw `InvalidCastException`. So does `Quantity = 50.0` on an `int`. Lists convert any number that fits. Decision 008.3 says the two match, but they only match for integer sizes. Clients usually send JSON numbers without a type, so `1` for a price is a likely request. Proposed: single values use the same `ConvertNumber` as lists. This overlaps with Phase 4's `ValueResult2` tidy-up.
- [x] **Decided and implemented (2026-11-23, decision 013.2):** a missing value with `Equals`, `NotEquals` or a comparison now throws `FilterConditionAndValueNotSupportedException` for every type, nullable properties included (use `IsEmpty`). A `ValueResult2` holding null counts as missing too. The original finding: **a missing value means different things per type** (found 2026-11-23). With `SearchValue = null` and a condition other than `IsEmpty`/`IsNotEmpty`: strings and enums throw `FilterConditionAndValueNotSupportedException`; numbers, `bool` and `Guid` silently compare with the default (`Quantity = null` → `Quantity = 0`, `IsActive = null` → `false`); dates throw a raw `InvalidOperationException` from `Nullable<T>.Value`. Proposed, in the spirit of 009: always throw `FilterConditionAndValueNotSupportedException`. A client that means 0 can send 0.
- [x] QueryShaper and its EF add-on have **zero warnings** and now build with `TreatWarningsAsErrors` (2026-11-22). `GroupByKeys` slots became `object?`, and `CreateBaseSearchLambdaExpression` validates its lookups.
- [x] Clear the 11 nullable warnings in `lab/Data` (all CS8618 on EF entities) (2026-11-24). Fixed with `= null!`, which the models already used for their other non-nullable properties: 4 strings and the `Person` navigation in `Address`, and the `Person`/`Affilation` navigations and the two `Affiliation` collections elsewhere. `dotnet ef migrations has-pending-model-changes` reports no model changes, so no migration was needed. `lab/Data` now builds with `TreatWarningsAsErrors`, and the whole solution has zero warnings.

### Phase 3: Close the PropertySelectors gaps
- [x] Non-generic arithmetic with properties (2026-11-24). `(PropertySelector)"Age * 2"` threw, because the non-generic path parsed against `PropertySelector<object>`. `ArithmeticOperationParser` now builds through an operand factory and an operation factory. `Parse<T>()` creates `PropertySelector<T>`/`ArithmeticOperationSelector<T>`. The new `Parse()` creates the non-generic `PropertySelector`/`ArithmeticOperationSelector`, whose property paths resolve against the target when the expression is built. Tests run the same ten expressions through both paths (operators, negation, two properties, nested path, precedence, grouping). An unknown property in the non-generic form fails only when the expression is built, with `Expression.PropertyOrField`'s `ArgumentException` (see the error-message item below).
- [x] Avg return type follows the element type (2026-11-24). Doing it turned up a bigger bug: **Sum, Avg, Min and Max only worked on `int` collections.** Each one called a single `MethodInfo` taken with `.First(...)` from `typeof(Enumerable).GetMethods()`, which in reflection order is the `IEnumerable<int>` overload. So `long`, `decimal`, `double`, `float` and the nullable forms all failed with an `ArgumentException` when the expression was built. QueryShaper never hit it because every aggregate in its tests ran over `int`. All four now pick the overload for the element type (`Implementations.GetAggregateMethod`, cached), with Min/Max falling back to the generic overload for other comparable types (dates, strings). Avg's declared type is that overload's return type, so it is `decimal` for decimals, `float` for floats and `double?` for `int?`. The declared type now matches the built expression for every aggregate. The four fixed `MethodInfo`s were removed. Also fixed: `MinAggregationSelector` used `nameof(MaxAggregationSelector)` in its error message. Tests cover each element type through both selector forms, plus `double` and `decimal` aggregates through EF on SQLite.
- [x] **Aggregates over an empty collection** (found 2026-11-23; decided and implemented 2026-11-24, **decision 014**). Over non-nullable elements, `Min`, `Max` and `Avg` declared a non-nullable type (`int`, `double`). In memory, `Enumerable.Min/Max/Average` threw on an empty sequence. In SQL they returned `NULL`, which failed when EF materialized it into a non-nullable value (mapping `Reviews.Stars|$FN_AGGR_MAX` into an `int` property: "Nullable object must have a value"). `ApplySelect` and ordering already worked. All three now lift a non-nullable value element to nullable before aggregating (`.Select(x => (int?)x).Max()`) and declare the nullable type, so an empty collection gives `null` in memory and in SQL. `Sum` is unchanged (an empty sum is 0). Mapping rule 010.9 now rejects these aggregates into a non-nullable destination at registration. Tests: every element type through both selector forms on an empty collection, arithmetic on a lifted `Max`, and mapper registration/null results through EF on SQLite.
- [x] **A coalesce selector, so a caller can choose a default for null** (found 2026-11-24, follow-up to decision 014; designed and implemented 2026-11-24, **decision 015**). `Min`, `Max` and `Avg` are now nullable, so mapping `Reviews.Stars|$FN_AGGR_MAX` into an `int` property is rejected at registration, and the selector language has no way to say "or 0 if empty". Decided: a variadic `$FN_COALESCE` in the dependent form, where the piped value is the first operand: `a|$FN_COALESCE(b, c, 0)` → `a ?? b ?? c ?? 0`. Every argument is a selector (a literal default, a property, a chain), resolved against the scope root. Every operand except the last must be nullable, otherwise it is rejected (also a non-nullable piped value). Types unify like C#'s `??` (numbers widen), and the result is nullable only if the last operand is. Collections are rejected. To implement:
  - [x] `CoalesceSelector<T>` / `CoalesceSelector` in `NullSelectors/`, with one overload for any argument count ≥ 1, and the right fold `Coalesce(o0, Coalesce(o1, …))` (2026-11-24). The generic form validates when parsed, as far as the types are known; both forms validate fully when the expression is built.
  - [x] Declared type when the selector is parsed: exact when every fallback is a literal. With a property fallback it is `null` until the expression is built, except for a reference-type subject (`string`), which declares its own type. The mapper (010.9) is unaffected because it checks the built expression.
  - [x] Tests (2026-11-24): 47 in `CoalesceSelectorTests`, through both selector forms (aggregate over an empty collection, nullable properties with literal and property fallbacks, several operands, widening, chaining, as a function argument, and every rejection). 5 in `CoalesceFilterTests` on SQLite (filters, ordering, `COALESCE` in `ToQueryString()`). 3 in `MapperTests` (010.9 accepts `…|$FN_COALESCE(0)` into an `int` destination and still rejects a nullable last operand).
  - [x] Documented in `property-selectors.md` ("Coalesce: a default for null").
- [ ] **Typed function arguments** (found 2026-11-24 while designing decision 015). `TryGetParameters` parses every argument as a non-generic `PropertySelector`, so a property argument has no `SelectorType` when the selector is parsed. Only literals are typed. Effects: `COALESCE` with a property fallback has no declared type until the expression is built, so a function chained after it fails in the generic form. **`MATH_MAX`/`MATH_MIN` throw when the selector is parsed if no argument has a known type** (confirmed 2026-11-24): `$FN_MATH_MAX(A, B)` with two properties, or with an argument that is itself a function, fails in the generic form with "No valid numeric types found in the list". It also blocks the item below. It needs the root type passed down to argument parsing. In the dependent form, `T` is the piped type, not the root type. Ties into decision 016.
- [x] **String literals are not opaque to the parser** (found and fixed 2026-11-24). Quoted text is now skipped by the three scanners that look for structure: the pre-scan (`AnalyzePropertySelector`: operators, `|`, parentheses), the argument splitter (`TryGetParameters`: commas, parentheses) and the arithmetic tokenizer (operands, plus a quoted literal as an operand of its own). A literal runs to the next `"`, with no escapes; an unclosed quote swallows the rest of the text. Tests: 14 cases through both selector forms in `QuotedLiteralTests` (`"n/a"`, `"a|b"`, `"(x"`, quoted arguments with commas and parentheses, quoted arguments inside arithmetic), and `Nickname|$FN_COALESCE("n/a")` back in `CoalesceSelectorTests`. 15 of the 16 failed on the old code; `"a, b"` on its own already worked. The original finding: `"n/a"` (and any literal with `+ - * /`) was parsed as arithmetic and failed with `Invalid char: "`. Correction to the original finding: `$VAL_STR:` as a function argument was not affected by operators, because each argument is parsed on its own and a `$VAL_` part is opaque. It still can't contain a comma or parentheses as an argument, since the payload has no end marker. Use a quoted string for that.
- [x] **In arithmetic, a chain that starts with a property can't contain a function with arguments** (found and fixed 2026-11-24). The property branch now tracks parenthesis depth once a `|` segment starts with `$`, so whitespace, commas and operators inside the arguments stay in the operand. Tests: 5 expressions through both selector forms in `ArithmeticOperationSelectorTests` (operand on either side, inside a group, arithmetic inside the arguments), plus `First|$FN_COALESCE(0) + 1` in `CoalesceSelectorTests`. All 7 failed on the old tokenizer. The original finding (found 2026-11-24): `ArithmeticOperationTokenizer.ReadSelector` has two operand branches. One starting with `$` tracks parenthesis depth, so `$FN_MATH_POW(2, 3) * 2` works (tested). One starting with a property name stops at the first `(`, so `First|$FN_COALESCE(0) + 1` is split into `First|$FN_COALESCE`, `(`, `0`, `)`, and the function gets no arguments. Chains whose functions take no arguments work (`Numbers|$FN_AGGR_MAX + 1`). Fix: in the property branch, once a `|` segment starts with `$`, read balanced parentheses the way the `$` branch does.
- [ ] Type-aware overload resolution.
- [ ] Implement `$FN_AGGR_COUNTDISTINCT`, and either finish or delete `SyntheticFunction`.
- [ ] Clear error messages that give the position and token for invalid selectors (a `SelectorParseException`). This includes the non-generic path: an unknown property there surfaces as `Expression.PropertyOrField`'s `ArgumentException` ("'Missing' is not a member of type …"), while the generic path says the property "was not found" (`InvalidOperationException`). The two should agree.
- [ ] (Optional) Unify the duplicated generic/non-generic record hierarchies.
- [ ] `PropertySelector<T>.ToString()` returns the compiler-generated record dump (`PropertySelector { PlainValue = … }`), because a derived record gets its own synthesized `ToString`, which hides the base's custom one. Fix: `public sealed override string ToString()` in `PropertySelector`. Affects `ToGeneric()` (which round-trips through `ToString`) and `OrderParameters<T>`'s base string (skipped test `TypedParameters_BaseStringIsTheFullSelector`).
- [ ] `PropertySelector<T>` type resolution unwraps `IGrouping<K, T>` to `T`, so `PropertySelector<IGrouping<K, T>>` can't resolve `Key` or root-level aggregates (`$FN_AGGR_COUNT`) and fails with an arity error. QueryShaper works around it with the non-generic selector (grouped `ApplySelect`).

### Phase 4: Shape the public API (the gate before any package ships)
- [x] Choose the product names and root namespaces, and rename the code (done in Phase 1).
- [ ] Audit `public` vs `internal`, and add XML docs to everything public.
- [ ] Rename temporaries (`ValueResult2`, `SearchValue.cs`, the `SyntheticFuncions` typo folder).
- [ ] Tidy `ValueResult2`'s value kinds (found 2026-11-23). Every integer (`byte`, `short`, `int`, `long`) is stored as `long` and reported as `ResultValueKind.Integer`, and `float` as `Double`. So the `Byte`, `Short`, `Long` and `Float` kinds are never produced, and the `ResultValueKind.Long` arm in the enum branch of `FilteringExtensions.cs` is dead code. Either remove those kinds or make `ValueResult2` report them. A related gap: there is no `ulong` conversion, so a single value above `long.MaxValue` (e.g. a `ulong`-backed enum member) can only be passed by name or as a numeric string. Lists accept a `ulong` directly.
- [ ] **A `Guid` filter with an invalid string silently becomes `Guid.Empty`** (found 2026-11-23). `ValueResult2`'s cast to `Guid` accepts a string and returns `Guid.TryParse(s, out var g) ? g : default`, so `Code = "not-a-guid"` filters on `Code = 00000000-…` instead of failing. This is the same kind of silent reinterpretation that decisions 009 and 013 removed. Proposed: an unparseable string throws `FilterConditionAndValueNotSupportedException`, and a valid one keeps working. Small, and can be done before the `ValueResult2` tidy-up.
- [ ] Remove the `OverflowException` catch in `FilteringExtensions.BuildConditionExpression` if nothing reaches it any more (found 2026-11-23). It turned `ValueResult2`'s checked integer casts into `FilterConditionAndValueNotSupportedException`. Since decision 013, numbers go through `ConvertNumber`, which throws that exception itself, and the remaining casts (`Guid`, `bool`, dates, the enum `long?` read) can't overflow. Confirm with the `ValueResult2` tidy-up, then delete it.
- [ ] Freeze the selector **grammar** and write it down as a spec (`docs/property-selectors.md` → "Grammar"). Settle D7 (decision 016) first.
- [ ] Add a QueryShaper request contract: a single `QueryRequest { Filters, Order, Page, Search }` that binds from JSON, plus an `Apply(request)` entry point.
  - Nothing binds from JSON yet. It needs System.Text.Json converters for `PropertySelector` (from a string; implicit operators are ignored), `ValueResult2` (from `JsonElement`; it has only private fields and constructors), and enum names (`JsonStringEnumConverter`).
  - `FilterNode` (decision 011) gets a converter that tells the two kinds apart by shape: `filters` present → `GroupNode`, otherwise `ConditionNode`, and both present → rejected. No `"type"` discriminator is needed. Built-in `[JsonPolymorphic]` is the alternative, but it requires the discriminator.
- [ ] Add a security note and guard rails: a property allow-list, max depth, and a page-size cap, because selectors arrive from untrusted clients.
- [ ] **Pass filter values as query parameters, not constants** (found 2026-11-23). The filters embed each client value with `Expression.Constant` (`FilteringExtensions.cs`, including the `In`/`ContainsAny`/`ContainsAll` value lists). The base search does the same (`BaseSearchConfiguration.cs`). EF Core includes constants in its query-cache key and inlines them into the SQL. So `Quantity = 50` and `Quantity = 51` are two separate queries, each compiled from scratch (often a millisecond or more), and the cache grows with every distinct value. That's the biggest per-request cost in QueryShaper, far more than any validation. It also means a high-cardinality filter, such as a search box, never hits the cache.
  - Fix: wrap each client value in a closure-style member access (e.g. a small `ValueHolder<T> { T Value }` passed through `Expression.Property(Expression.Constant(holder), "Value")`), which EF Core turns into a SQL parameter. Fixed values (`0`, `null`, `""` in `IsEmpty`) can stay constants. For value lists, a parameterized `List<T>` makes EF Core 8+ send them as a single JSON parameter (`json_each` on SQLite, `OPENJSON` on SQL Server) instead of inlining every element.
  - Verify: tests that assert `ToQueryString()` contains a parameter (`@__…`) instead of the literal value, plus one that runs the same filter with two different values and checks that EF compiled only one query (count `CoreEventId.QueryCompilationStarting` events through the test context's logger).
  - Optionally measure before/after in `lab/Benchmarks/`. This is not a public API change, but it belongs before the first release.
- [ ] Make the And/Or evaluation order configurable (decision 007 follow-up), once QueryShaper has a settled configuration approach (see D4). Proposed: `FilterEvaluationOrder { LeftToRight, AndBeforeOr }`, settable globally in `QueryShaperConfiguration` and overridable per call. Default `LeftToRight` (today's behaviour). Run `FilterNodeLogicTests` under both modes; only the ordering tests should differ.

### Phase 5: Package and release `0.1.0-preview`
- [ ] Package metadata (`PackageId`, description, license, README in the package, SourceLink, deterministic builds).
- [ ] Add `CHANGELOG.md` per package, use SemVer, and tag the repository per package (`propertyselectors-v0.1.0`).
- [ ] Publish PropertySelectors first, since it has no dependencies. Publish QueryShaper against the released PropertySelectors version.
- [ ] Add a samples project: a minimal ASP.NET endpoint that uses QueryShaper end to end.

### Phase 6: Later
- Source generator for selector discovery (trimming/AOT).
- Additional providers (beyond EF Core), and a projection/`Select` DSL.
- **Base search over every string property, as an explicit opt-in** (discussed 2026-11-23). Proposed: `ConfigureSearch<T>()` with no lookups searches every public string property of `T`, and `ConfigureSearch<T>(except: u => u.PasswordHash)` leaves named ones out. The property list is built and checked once at registration, and a type with no string properties throws then, at startup, not on the first search. An unconfigured type still throws (010.8).
  - Rejected for now: making "all string properties" the implicit default for unconfigured types. Search values come from untrusted clients, so every string field, including hashes, tokens and internal notes, would become searchable and could be guessed one character at a time from whether rows come back. Search results would also change whenever a string property is added. The core can't see EF metadata (006), so `[NotMapped]` properties, computed getters and converted columns would only fail on the first search. And an OR of `LIKE '%x%'` across every column is a costly default. If an implicit default is ever wanted, make it a global setting that is off unless an app turns it on.
  - Do it after the Phase 4 guard rails (property allow-list) and D4 settle the configuration approach. The `except:` list should use the same mechanism as the allow-list.
- Benchmarks published in the docs, and an optional docs site.
- Possibly split into separate repositories once the APIs are stable.

## 5. Open decisions (owner: you)

| # | Decision | Recommendation |
|---|---|---|
| D1 | Product/package names and namespace root | ✅ **Decided (005):** `Steverzag.PropertySelectors`, `Steverzag.QueryShaper`, `Steverzag.QueryShaper.EntityFrameworkCore`. |
| D2 | Target framework(s) | `net8.0` (LTS) + `net9.0`. That widens adoption, and the code uses nothing that needs 9 except possibly `SearchValues`, which exists in 8. |
| D3 | EF Core coupling | ✅ **Decided (006):** the core uses only `IQueryable`. EF Core-specific code lives in `Steverzag.QueryShaper.EntityFrameworkCore`. |
| D4 | `QueryShaperServices` static ambient registry | Keep it only as a fallback. Make the explicit `IMapper`/`IBaseSearchConfiguration` parameters the primary path, because static state breaks tests and multi-tenant setups. **Evidence (Phase 2):** `ConfigurationTests.TheStaticFallback_FollowsTheLastServiceCollection`: two containers, and the fallback silently follows whichever called `AddQueryShaper` last. A "nothing registered" test can't be written reliably. |
| D5 | Should `$VAL_*` specifiers start with `$SPEC_`? | `SpecifierStart` exists but nothing uses it. Remove it, or document `$VAL_` as canonical. |
| D6 | Monorepo vs. two repositories | Stay a monorepo until 1.0. |
| D7 | When a function takes its subject from the pipe (dependent) or from an argument (static) | **Open (016), to discuss.** Today's split follows the shape of the .NET call, except `DT_ADD*`, which is static although `AddDays` is an instance method. So `Created\|$FN_DT_ADDDAYS(3)` and `Price\|$FN_MATH_ROUND` look valid but aren't supported. Options: follow .NET, dependent for anything with one natural subject, or both forms everywhere with the 015 rule (the piped value is the first argument). Decide before the grammar freeze. |

Recorded decisions live in [decisions.md](decisions.md).
