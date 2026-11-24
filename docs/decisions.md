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
**Status:** accepted; implemented (2026-11-23). 5 via decision 011.
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
**Status:** accepted; implemented (2026-11-23)
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

### 012: A page is a read-only list (2026-11-23)
**Status:** accepted; implemented (2026-11-23)
**Context:** `PaginatedList<T>` inherited from `List<T>`, so callers could `Add`, `Remove`, `Clear` or replace items,
and the page would then disagree with its own `TotalCount` and `TotalPages`. `List<T>` has no virtual members, so a
subclass can't guard them.
**Decision:** `PaginatedList<T>` is a `sealed` class that implements `IReadOnlyList<T>` over a private copy of the
items, with get-only paging properties. It stays a collection, so `foreach`, LINQ, `Count` and the indexer keep working.
**Considered:** not being a collection at all, with an `Items` property. That serializes with the paging properties
included, where a collection serializes as a bare JSON array. Rejected: callers that want the paging properties in a
response wrap the page in their own type, and the library doesn't impose a response shape.
**Consequence:** a breaking change for code that mutated the page or relied on it being a `List<T>`; none in this
repository did. `Count` is the number of items on the page; `TotalCount` is the number across all pages.

### 013: Single filter values follow the same rules as lists (2026-11-23)
**Status:** accepted; implemented (2026-11-23)
**Context:** writing down decision 008 found two inconsistencies. A single number had to be the same kind as the
property (`1` for a `double` property threw `InvalidCastException`), while lists converted any number that fits.
And a missing value (`SearchValue = null`) meant different things per type: strings and enums threw a filter error,
numbers, `bool` and `Guid` compared with the default (`Quantity = null` matched `Quantity = 0`), and dates threw a bare
`InvalidOperationException`.
**Decision:**
1. **A single number converts like a list value.** Any number that fits the property is accepted, with the same
   `ConvertNumber` rules: `1` for a `double` or `decimal`, `50.0` for an `int`. A fractional part, NaN, infinity or an
   out-of-range value for an integer property throws `FilterConditionAndValueNotSupportedException`. A non-number
   (a numeric string, `bool`) still throws `InvalidCastException`.
2. **A condition that compares with a value needs one, for every type.** `null` (or a `ValueResult2` holding null)
   with `Equals`, `NotEquals` or a comparison throws `FilterConditionAndValueNotSupportedException`. This also applies to
   nullable properties: to match nulls, use `IsEmpty`. A client that means 0 or `false` sends 0 or `false`.
**Consequence:** a behaviour change. Requests that relied on `null` meaning 0 or `false` now fail. Clients sending
JSON numbers no longer need to match the property's number kind. The seven numeric branches in `FilteringExtensions`
became one. 008.3's statement that lists "match single values" now holds.

### 014: Min, Max and Avg are nullable, so an empty collection gives null (2026-11-24)
**Status:** accepted; implemented (2026-11-24)
**Context:** over a collection of non-nullable values, `AGGR_MIN`, `AGGR_MAX` and `AGGR_AVG` declared a non-nullable
type (`int`, `double`). An empty collection broke that in two ways. In memory, `Enumerable.Min/Max/Average` threw
"Sequence contains no elements". In SQL the aggregate is `NULL`, so mapping it into an `int` property registered fine
and then failed when EF materialized the row ("Nullable object must have a value"). `ApplySelect` and ordering already
worked, because they box the value or let SQL sort `NULL`.
**Decision:** lift a non-nullable value element to its nullable form before aggregating
(`x.Select(e => (int?)e).Max()`) and declare the nullable result type: `int` → `int?`, `DateTime` → `DateTime?`,
`AVG` of `int` → `double?`. An empty collection gives `null` in memory and in SQL. `AGGR_SUM` is unchanged: an
empty sum is 0 in memory, and EF Core wraps `SUM` in `COALESCE(…, 0)`. Reference elements (`string`) are unchanged;
their `Min`/`Max` already return `null`.
Rejected: turning an empty collection into `default(T)` (0 stars for a product with no reviews) is the kind of silent
reinterpretation that 009 and 013 removed.
**Consequence:** a breaking change in declared types. Code that reads these aggregates as non-nullable must use the
nullable type. Mapping rule 010.9 now rejects `MIN`/`MAX`/`AVG` into a non-nullable destination at registration,
instead of failing on the first row with an empty collection. Arithmetic on them stays nullable
(`Numbers|$FN_AGGR_MAX + 1` is `int?`).

### 015: `$FN_COALESCE` is a variadic function whose piped value is the first operand (2026-11-24)
**Status:** accepted; implemented (2026-11-24)
**Context:** since 014, `MIN`, `MAX` and `AVG` are nullable, so mapping `Reviews.Stars|$FN_AGGR_MAX` into an `int`
property is rejected at registration (010.9). The selector language had no way to say "or 0 if empty", so the only
workaround was an `int?` destination coalesced after the query. The same need shows up outside aggregates: "the last
update, or the creation date if there was none".
**Decision:**
1. **One token, `$FN_COALESCE`, written in the dependent form.** The piped value is the first operand, and the
   arguments are the fallbacks in order: `a|$FN_COALESCE(b, c, 0)` means `a ?? b ?? c ?? 0`.
   - `Reviews.Stars|$FN_AGGR_MAX|$FN_COALESCE(0)` → `p.Reviews.Select(r => (int?)r.Stars).Max() ?? 0`, declared `int`.
   - `LastUpdated|$FN_COALESCE(Created)` → `p.LastUpdated ?? p.Created`.
2. **Every argument is a selector.** A default is just a value selector (`0`, `"n/a"`, `$VAL_DATETIME:…`), and a
   fallback can be a property, a chain or a nested function. Arguments are resolved against the scope root, like the
   arguments of the instance string functions (`STR_CONTAINS`).
3. **Nullability.** Every operand except the last must be able to be null (`Nullable<>` or a reference type).
   Otherwise the selector is rejected, because the operands after it can never be reached. This includes a
   non-nullable piped value: `Quantity|$FN_COALESCE(0)` on an `int` is an error, not a no-op. C# rejects the same thing
   (CS0019), and a silent no-op would hide a wrong assumption about the field.
4. **Result type.** The underlying types are unified the way C#'s `??` does it. Numbers widen with
   `GetWiderNumericType` (`int? ?? long` → `long`, `decimal? ?? 0` → `decimal`). Other types must match exactly
   (`DateTime?` with `DateTime`, `string` with `string`). Anything else is rejected. The result is nullable if, and only
   if, the last operand is nullable, so `a ?? b` with both nullable stays nullable and 010.9 still rejects it into a
   non-nullable destination.
5. **Expression shape.** A right fold, `Coalesce(o0, Coalesce(o1, … on))`, with each operand converted to the unified
   type first. EF translates it to `COALESCE(a, COALESCE(b, 0))`, and in memory it short-circuits the same way.
6. **Collections are rejected** as operands (`Phones.Number|$FN_COALESCE(…)`): coalescing a sequence has no meaning.
7. **Delivery: literal defaults first, typed arguments later.** Function arguments are parsed as non-generic
   selectors, so a property fallback has no type until the expression is built. With a literal last operand (the
   main case) the declared type is exact when the selector is parsed. With a property fallback the declared type
   (`Type`) is `null` until the expression is built, except for a reference-type subject such as `string`, whose
   type can't change. 010.9 is unaffected, because the mapper checks the built expression's type. What the
   limitation does block is chaining a function after a value-type coalesce with a property fallback, in the generic
   form. Typed arguments (the root type passed down to argument parsing) are a separate roadmap item, shared with
   type-aware overload resolution.

**Considered:**
- **The static form, `$FN_COALESCE(a, b, 0)`.** It reads like SQL, but the dependent form is enough: it covers every
  case (each argument is a full selector, so `$FN_MATH_MAX(A|$FN_COALESCE(0), B)` works). It also fits the pipe, so a
  default goes after an aggregate without wrapping it in parentheses, and the subject's type comes from the chain.
  Allowing the static form at the head, as sugar for the same rule, is left open: it makes the token depend on its
  position, and the language has no consistent rule yet for when a function takes its subject from the pipe or from
  an argument (see 016).
- **A mapper-only `ForMember(…, defaultValue: 0)`.** Simpler, but it only helps mapping, not filters, ordering or
  `ApplySelect`.

**Consequence:** the caller writes the default explicitly, which fits 009, 013 and 014: they removed *silent*
defaults, not explicit ones. 010.9 accepts `…|$FN_COALESCE(0)` into a non-nullable destination. The function works
anywhere a selector does. It is the first variadic function: one overload for any count ≥ 1, instead of listing each
count by hand as `STR_CONCAT` does.

### 016: When a function takes its subject from the pipe or from an argument (2026-11-24)
**Status:** open; to be discussed before the Phase 4 grammar freeze
**Context:** a function can get the value it operates on in two ways:
- **Dependent:** the subject is the piped value, and the arguments are the extra inputs.
  `Name|$FN_STR_CONTAINS("x")` → `p.Name.Contains("x")`. The arguments are resolved against the scope root
  (`FindRootParameter`).
- **Static:** the subject is one of the arguments, and the function is usually the head of its chain.
  `$FN_MATH_ROUND(Price, 2)` → `Math.Round(p.Price, 2)`. The arguments are resolved against the target, which is the
  root when the function is the head of the chain.

There is no written rule for which a function uses. The functions as they stand:

| Form | Functions |
|---|---|
| Dependent | aggregation (`AGGR_*`), element (`ELM_*`), ordering (`ORDER_*`, `THEN_ORDER_*`), `STR_LENGTH`, `TOUPPER`, `TOLOWER`, `TRIM`, `TRIMSTART`, `TRIMEND`, `SUBSTRING`, `INDEXOF`, `REPLACE`, `CONTAINS`, `STARTSWITH`, `ENDSWITH`, and `COALESCE` (015) |
| Static | every `MATH_*`, `STR_CONCAT`, `STR_JOIN`, `STR_COMPARE`, `STR_ISNULLOREMPTY`, `STR_ISNULLORWHITESPACE`, `DT_ADD*` |
| No subject | `DT_NOW`, `DT_UTCNOW`, `DT_TODAY` |

The pattern behind it is "follow the shape of the .NET call": instance and extension methods are dependent, and
static methods are static. `DT_ADD*` breaks that pattern: `DateTime.AddDays` is an instance method, but
`$FN_DT_ADDDAYS(Created, 3)` takes its subject as an argument, while `Name|$FN_STR_TRIM` doesn't. A caller can't
predict the form from the function's name, and `Price|$FN_MATH_ROUND` and `Created|$FN_DT_ADDDAYS(3)` look valid but
are not supported.
**To decide:**
1. **The criterion.** Options: (a) follow .NET (then `DT_ADD*` becomes dependent); (b) follow the language's own
   rule: anything with a single natural subject is dependent, and only functions whose operands are symmetric are
   static (`CONCAT`, `MATH_MAX`, `MATH_POW`, `STR_COMPARE`); (c) support both forms for every function, with the rule
   from 015: in the dependent form the piped value is the first argument (`Price|$FN_MATH_ROUND(2)` ≡
   `$FN_MATH_ROUND(Price, 2)`).
2. **If both forms are allowed, how the parser tells them apart.** At the head of a chain the target is the root
   parameter, which must not become an operand. The selector needs to know whether it is the head; today it can't.
3. **Typing.** In the dependent form the subject's type comes from the chain, but the arguments' types are unknown
   when they're parsed. At the head of the chain the root type is known, so the arguments could be typed. Whatever
   is chosen should fit typed arguments and type-aware overload resolution.
4. **Migration.** Which existing functions change form, and whether the old form is kept as an alias until the
   grammar is frozen.
