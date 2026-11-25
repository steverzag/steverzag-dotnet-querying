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
**Status:** accepted and implemented (2026-11-24), as proposed below except §7 (revised) and §10 (deferred)
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

**Decision (proposed and accepted 2026-11-24):** option (c) in one direction only. Every function that has a subject accepts the piped
form. The static form stays where it exists today and is not added anywhere else.
1. **The rule: the piped value is the first argument.** This is 015's rule applied to every function.
   `Price|$FN_MATH_ROUND(2)` ≡ `$FN_MATH_ROUND(Price, 2)`, and `Created|$FN_DT_ADDDAYS(3)` ≡
   `$FN_DT_ADDDAYS(Created, 3)`. A caller who knows the static form knows the piped one: take the first argument
   out and put it in front of `|`. The two forms build the same expression.
2. **Which functions gain the piped form:** every function in the static row of the table that has a subject: all of
   `MATH_*`, `STR_CONCAT`, `STR_COMPARE`, `STR_ISNULLOREMPTY`, `STR_ISNULLORWHITESPACE` and `DT_ADD*`. Symmetric
   functions are included, even though they have no natural subject, because the rule is mechanical and needs no
   exceptions: `A|$FN_MATH_MAX(B)` is `Math.Max(A, B)`, and `First|$FN_STR_CONCAT(" ", Last)` is
   `string.Concat(First, " ", Last)`.
3. **Which functions reject the piped form:**
   - functions with no subject: `DT_NOW`, `DT_UTCNOW`, `DT_TODAY`;
   - the literal constructors `DT_DATETIME`, `DT_DATEONLY`, `DT_TIMEONLY` (017.5), whose argument must be a string
     literal;
   - `STR_JOIN`. Its first argument is the separator, so the mechanical rule would make `Tags|$FN_STR_JOIN(", ")`
     mean `string.Join(Tags, ", ")`. Rejected, not reordered: a special case would break the "take the first
     argument out" reading.

   The error says the function takes no piped value and shows its static form.
   - *Implemented as (2026-11-24):* the `[SelectorToken]` attribute declares each function's piped form with a
     `PipedForm` enum: `Target` (the default, the dependent functions), `FirstArgument` (§2) or `Rejected` (§3), plus
     an optional `PipedHint` sentence for the rejection message. External selectors default to `Target`, which is
     the behaviour they had before 016.
4. **Dependent functions do not gain a static form.** `$FN_STR_TRIM(Name)` stays invalid. At the head of a chain a
   dependent function takes the root as its subject, and QueryShaper relies on that (grouped `ApplySelect` writes
   `$FN_AGGR_COUNT` to count the group). With a static form as well, the head would be ambiguous for overloaded
   functions: on a `string` root, `$FN_STR_SUBSTRING(1, 2)` could be `root.Substring(1, 2)` or a subject `1` with
   start `2`. Telling them apart needs typed arguments and type-aware overload resolution. This direction can be
   reconsidered once both exist. The grammar freeze doesn't need it, because adding a form later breaks nothing.
5. **Arguments resolve against the scope root in both forms.** In the piped form only the subject comes from the
   pipe. The other arguments resolve the way a dependent function's arguments do (`FindRootParameter`):
   `Details|$FN_MATH_ROUND(Precision)` reads `p.Precision`, not `p.Details.Precision`. Today a static function chained
   after a `|` resolves its arguments against the piped value (`ExpressionUtils.CreateMemberAccess(target, …)`). No
   test covers that, and it disagrees with every dependent function, so it changes.
6. **How the parser tells the forms apart.** By position, not by argument count: a function that is not the first
   part of its chain is piped. The chain loops in both implicit operators already know this (`result is null`), and
   they pass it through `TryResolveGeneric`/`TryResolveNonGeneric`. A piped function gets a placeholder that stands
   for the piped value as argument 0, so the existing overloads match unchanged.
7. **Arguments are resolved by one shared helper, `ExpressionUtils.ResolveArgument`** (revised 2026-11-24 while
   implementing). The placeholder becomes the piped value, and every other argument resolves against the scope root
   recovered from the target with `FindRootParameter`, the way dependent functions already do it. The overload
   signature (`Func<Expression, Expression>`) is unchanged, and so are the function files. The static helpers resolve
   their arguments in only 8 places, all in the math, date and string `Implementations.cs`.
   - *Originally proposed:* a new overload signature that receives the piped value and the root separately, a
     one-line change in each of the 61 function files. Dropped because the helper does the same with 8 edits and no
     mutable state.
   - *Also rejected:* capturing the piped value in the base class for each build. It puts mutable state in records
     that may be cached and shared.
   - *Limit, shared with dependent functions:* the root is recovered from the target, so it is lost when the target
     doesn't come from a parameter. In `$FN_DT_NOW|$FN_DT_ADDDAYS(Days)`, `Days` is looked up on `DateTime`.
     Passing the real root down through `PropertyAccess` would fix both kinds, but it is a larger change.
8. **Typing.** In the generic form the subject's type is the piped type (`currentIterationType`), which is known when
   the selector is parsed. So `Created|$FN_DT_ADDDAYS(3)` declares its real type (`DateOnly`, `DateTimeOffset`)
   right away. The static form still waits on typed arguments for that.
9. **`ToString()` writes the form that was parsed.** Both forms are canonical, and the text round-trips as written.
   Normalizing to one form would rewrite what a caller saved, for no gain.
10. **The metadata tree** shows the piped form on every function that accepts it, so a UI can offer
    `Price → Round` the same way it offers `Name → Trim`.
    - *Deferred (2026-11-24):* the metadata tree lists only properties, not functions, so there is nothing yet to
      add the piped form to. Moved to the Phase 4 item "Function metadata for UIs", which includes it.
11. **The new date functions follow the same rule.** The parts (`DT_YEAR` …) and truncations (`DT_DATE` …) have
    one natural subject, so they are written piped first, `Created|$FN_DT_YEAR`, with the static form
    `$FN_DT_YEAR(Created)` as well, matching `DT_ADD*`. Two-date differences are symmetric, like `MATH_MAX`.

**Considered:**
- **(a) Follow .NET.** It's predictable for .NET developers, but `DT_ADD*` would have to move to the dependent form,
  breaking `$FN_DT_ADDDAYS(Created, 3)`. And `Math.Round` stays static, so `Price|$FN_MATH_ROUND` would still fail.
  The caller still needs to know where each .NET method lives.
- **(b) Dependent for anything with one natural subject.** It reads best, but it moves most of `MATH_*` and
  `DT_ADD*` to a new form, and "natural subject" is a judgement call the caller has to share (`MATH_POW`? `MATH_LOG`
  with a base?).
- **(c) in both directions.** It gives the most freedom, but it needs typed arguments to resolve the head ambiguity
  (§4), so the grammar freeze would wait on that work. It can be added later without breaking anything.

**Consequence:** a caller can write every function with a subject left to right: `Price|$FN_MATH_ROUND(2)`,
`Created|$FN_DT_ADDDAYS(3)`, `Reviews.Stars|$FN_AGGR_MAX|$FN_COALESCE(0)`. Nothing that parses today stops parsing,
except a static function chained after a `|` that relied on resolving its arguments against the piped value (§5),
which no test covers. Work: the piped flag through the registry, the placeholder argument, a shared argument
resolver in the static helpers (§7), `ToString()` and the metadata tree, and tests for each function in both forms,
generic and non-generic. It does not depend on typed function arguments.

### 017: Literals are written plainly; `$VAL_*:` specifiers are removed (2026-11-24)
**Status:** accepted; implemented (2026-11-24)
**Context:** a literal can be written in two ways. The original form is a specifier, `$VAL_INT:5` or
`$VAL_DATETIME:2024-01-01T00:00:00`. Bare literals (`5`, `1.5`, `true`, `"text"`) came later, and the two now coexist.
Fixing `ToString()` (Phase 3) showed what the specifier form costs:
- **Its value has no end marker.** Everything after `$VAL_X:` belongs to the literal, so `$VAL_INT:10 - A` is read
  as the int `10 - A`, and `-(A - $VAL_INT:10) / 2` fails the same way. As a function argument it can't contain a
  comma or parentheses. Every place that scans structure (the pre-scan, the argument splitter, the arithmetic
  tokenizer) would need its own rule for where the value ends.
- **Two forms with different rules.** `$VAL_` values are read with the invariant culture, while bare numbers are read
  with the current culture. So `A * 1.5` means `A * 15` on a machine set to es-ES, but `A * $VAL_DOUBLE:1.5` doesn't.

Nothing outside PropertySelectors uses the specifiers (QueryShaper and the metadata tree don't), and nothing has
shipped, so removing them costs only work, not compatibility.

Specifiers really give only two things: an explicit type, and a value that may hold any character. Everything else is
better done by bare literals, except dates and times. They can't be written bare: `2024-01-01` reads as subtraction,
and a date-time and a date-only value would look the same.

**Decision:**
1. **Numbers are bare and read with the invariant culture.** An integer is `int` if it fits and `long` otherwise, as
   in C#. A number with a `.` or an exponent is `double`. Suffixes pick other types: `L` for `long`, `m` for
   `decimal`. `long` and `decimal` literals are new.
2. **A minus sign directly in front of a number is part of the literal** wherever a unary minus could stand:
   `A * -2` holds the int `-2`. Today it becomes a negation, which is typed `double` like all arithmetic. A minus
   before anything else (`-A`, `-(A + B)`) is still negation.
3. **Booleans are `true` and `false`.** Parsing stays case-insensitive, as today. `ToString()` writes lowercase.
4. **Strings are quoted, and a `"` inside is doubled**: `"say ""hi"""` is `say "hi"`. The scanners that skip quoted
   text need no change: a doubled quote closes the literal and reopens it at once, so the characters inside stay
   skipped. Only the literal's value has to turn `""` into `"`.
5. **Dates and times are constructor functions taking one quoted string**: `$FN_DT_DATETIME("2024-01-01T08:30:00")`,
   `$FN_DT_DATEONLY("2024-01-01")` and `$FN_DT_TIMEONLY("08:30")`. They are named after the .NET types, not
   `DT_DATE`/`DT_TIME`, so that those names stay free for the parts of a date (§9: `DT_DATE` as `DateTime.Date`). The
   string is read with the invariant culture, and a date-time keeps its kind (`DateTimeStyles.RoundtripKind`: a
   trailing `Z` stays UTC instead of being converted to local time). The argument must be a string literal, and it is
   checked when the selector is parsed, so a bad date fails then and not when the expression is built. The result is
   a constant, like any other literal.
6. **`$VAL_*:` is removed, with no alias.** That means `ValueStart`, `ValuePrefix`, the seven value specifiers, and
   the code paths that treat a `$VAL_` value as opaque. `SpecifierStart` and the non-generic parser's unused specifier
   branch go too, which settles D5.
7. **`ToString()` writes these forms** (§1–5). Every literal then reparses wherever it stands, which closes the
   round-trip gap that the `ToString()` fix left open for doubles and dates.
8. **The library formats literals for callers.** A public helper (name to be chosen, e.g.
   `SelectorLiteral.Format(object)`) turns a .NET value into its literal text, so .NET code that builds selectors never
   writes quoting or date formatting by hand. `ToString()` uses the same code.
9. **Dates and times are a function family, and the goal is a broad catalog.** The three constructors are the first
   step. The family should end up covering most of what callers do with dates in .NET, under the `DT_` prefix:
   - **Construct:** `DT_DATETIME`, `DT_DATEONLY`, `DT_TIMEONLY` (this decision).
   - **Current time:** `DT_NOW`, `DT_UTCNOW`, `DT_TODAY` (they exist).
   - **Add:** `DT_ADDYEARS` … `DT_ADDMILLISECONDS` (they exist; their form, piped or static, waits for 016).
   - **Parts:** `DT_YEAR`, `DT_MONTH`, `DT_DAY`, `DT_HOUR`, `DT_MINUTE`, `DT_SECOND`, `DT_MILLISECOND`,
     `DT_DAYOFWEEK`, `DT_DAYOFYEAR`.
   - **Truncate and convert:** `DT_DATE` (`DateTime.Date`), `DT_TIMEOFDAY`, `DT_TODATEONLY`, `DT_TOTIMEONLY`.
   - **Differences:** days, hours and so on between two dates.

   Each function should work on `DateTime`, `DateOnly` and `TimeOnly` wherever .NET has the member, and must
   translate to SQL. Differences need care: the core can't reference EF Core (006), and providers translate
   `(a - b).TotalDays` differently. The list is a target, not a contract, and it grows on the roadmap.

**Considered:**
- **Keep `$VAL_` and give its value an end** (stop at the operand's boundary). Rejected: each new context, such as
  arguments, arithmetic or a future grammar feature, would need the boundary defined again. That is the class of bug
  this decision removes.
- **Keep `$VAL_` for code that generates selectors.** The argument for it: a client that builds selector text from a
  value, such as a grid whose date picker writes `"$VAL_DATETIME:" + date.toISOString()`, only joins two strings. It
  needs no escaping and no knowledge of the grammar. Rejected, for three reasons:
  - **The ease is only apparent.** Because the value has no end, the generated text breaks as soon as it is not the
    last token: inside arithmetic, or as an argument containing a comma or parenthesis. A generator has to know those
    rules anyway.
  - **Generating the new forms is just as easy.** A date is `'$FN_DT_DATETIME("' + iso + '")'`, and an ISO date never
    contains a quote. A string needs one rule (double every `"`), and .NET callers get §8.
  - **Values typed by end users don't belong in selector text.** A selector names *what* to read (fields, functions,
    the occasional fixed constant). The values users type, such as a search term or a filter bound, travel as
    QueryShaper filter values (`SearchValue`), which are never parsed as grammar. Building selector text from user
    input is an injection risk under any literal syntax. The Phase 4 security note should say so.
- **A prefix on the quote** (`d"2024-01-01"`, `t"08:30"`). It's shorter, but it is new grammar for the tokenizer and
  the metadata tree. Constructor functions reuse the registry, argument parsing and metadata that already exist.
- **Escaping with `\"`.** Rejected: it brings backslash rules (`\\`, and what `\n` means), and the scanners would have
  to track escapes. Doubling needs neither.

**Consequence:** a breaking change in the selector grammar, made before any release. Selectors using `$VAL_` must be
rewritten (`$VAL_INT:5` → `5`, `$VAL_STR:x` → `"x"`, `$VAL_DATETIME:…` → `$FN_DT_DATETIME("…")`,
`$VAL_DATE:…` → `$FN_DT_DATEONLY("…")`, `$VAL_TIME:…` → `$FN_DT_TIMEONLY("…")`). A bare number now
means the same on every machine. The tests that pin `$VAL_` forms (mainly `ValueSelectorTests`, `SelectorDefaultsTests`
and `DateSelectorTests`) are rewritten to the new forms.

### 018: Function arguments are typed when the selector is parsed (2026-11-25)
**Status:** accepted; implemented (2026-11-25)
**Context:** every function argument was parsed in the non-generic form (`TryGetParameters` cast each one to
`PropertySelector`), so in the generic form a property argument had no type until the expression was built. Effects:
`$FN_DT_ADDDAYS(Day, 3)` declared `DateTime` while building `DateOnly` (and `DateTimeOffset` on a `DateTimeOffset`);
`COALESCE` with a property fallback declared `null`, so a function chained after it failed in the generic form;
`MATH_MAX`/`MATH_MIN` with a property argument declared `null`. It also blocks type-aware overload resolution and
function metadata (Phase 4). Arguments don't all resolve against the same type: most functions resolve them against
the scope root (`ExpressionUtils.ResolveArgument`), while the ordering functions resolve them against the element of
their target inside a lambda.

**Decision:**
1. **In the generic form, arguments are parsed in the generic form against their scope type.** The registry parses
   each argument as `PropertySelector<TScope>` through a delegate cached per scope type. Nested arguments,
   arithmetic and literals inside an argument are typed by the same code that types the rest of the generic form.
2. **Each function declares its argument scope** on `[SelectorToken]`: `Arguments = ArgumentScope.Root` (the
   default) or `ArgumentScope.TargetElement` (the four ordering functions). The scope root is the generic form's
   `T`, which `TryResolveGeneric` now receives next to the piped type. Inside an ordering argument the element
   becomes the root for anything nested in it. If the target of a `TargetElement` function isn't a collection, its
   arguments are parsed untyped, as before.
3. **The non-generic form is unchanged.** It has no root type, so its property arguments stay untyped until the
   expression is built. `ToGeneric<T>()` reparses the text and types them.
4. **Errors move earlier in the generic form.** An unknown property inside an argument fails when the selector is
   parsed (`InvalidOperationException`, "Property 'X' was not found on type 'T'"), like the head of a generic chain
   already did. A coalesce mismatch with a property fallback is also rejected then.
5. **Structural errors come first.** If typing an argument fails, the function is still built from the untyped
   argument, so the argument-count and piped-form checks run and report their own messages. Only if the function is
   valid is the argument error rethrown. Without this, `Inner|$FN_MATH_MAX(A, 1)` would report that `A` isn't on the
   root, which hides the real mistake (the subject repeated as an argument).
6. **No guessed types.** `DT_ADD*` declared `Parameters[0].Type ?? typeof(DateTime)`. It now declares the
   subject's type or `null`, so the declared type is never wrong.
7. **Type-aware overload resolution is a separate step.** Overloads still match by argument count. Typed arguments
   are its prerequisite.

**Considered:**
- **Probe-building** each argument against `Expression.Parameter(scopeType)` and reading `.Type`. It's exact by
  construction and a small change, but it builds expression trees while parsing, and stores the type next to the
  argument instead of in it, so the argument is still an untyped selector.
- **A separate type-inference pass.** It doesn't build anything, but it duplicates the builder's rules, and that
  drift has already caused three bugs in Phase 3 (aggregates, math, arithmetic).
- **Leaving arguments untyped** and only removing the `DateTime` guess. Fixes one bug and blocks the rest.

**Consequence:** in the generic form every built-in function declares the type it builds, in both the piped and
static forms (tests check declared = built for each). `ElementAt` reads its index through an `IIntegerLiteralSelector`
interface, because the literal is now the generic record. External selectors default to `ArgumentScope.Root`, which
matches `ResolveArgument`. A selector that resolves arguments elsewhere must declare its scope, or its arguments are
typed against the wrong type and may be rejected when parsed.

### 019: Function overloads are declared in signature tables and resolved by type (2026-11-25)
**Status:** accepted; implemented (2026-11-25)
**Context:** each function built only the overload for the argument count it was given (`InitializeOverloads`
branched on `Parameters.Count`), and every parameter was declared as `typeof(PropertySelector)`. So nothing could
list a function's overloads (Phase 4 metadata), type errors surfaced only at build time from helper-specific checks,
and two overloads with the same count couldn't coexist. That last point is needed for a static form of the dependent
functions (016.4), a `DT_ADD` taking a `TimeSpan`, date differences, and possibly `STR_CONTAINS(char)`. No built-in
function has same-count overloads yet. Each function also computed its declared type by hand, separately from what it
builds, and that drift had already caused several bugs.

**Decision:**
1. **Each function declares all its overloads up front** in a static table of `FunctionSignature`s, shared by the
   generic and non-generic records. A signature has parameter constraints, an optional `Target` constraint (for
   functions that work on the piped value), an optional variadic `Rest`, a result-type rule
   `(targetType, argumentTypes) → Type?`, and `Bind`, which turns the arguments into the behaviour. `Bind` runs when
   the overload is resolved, so it can also validate (the date literals parse their text there).
2. **Constraints are kinds, not exact types:** `Any`, `Numeric`, `Integer`, `String`, `Bool`, `DateLike`,
   `Collection`, `Of(type)`, and the literal-only `StringLiteral` and `IntegerLiteral`. A nullable type matches like
   its underlying type.
3. **Resolution:** by count first (no match → `ArgumentException`, now saying which counts are accepted), then by
   constraints (no match → `InvalidOperationException` naming the argument, its type and what was expected). If one
   candidate is left, it wins. Otherwise the C#-like rule applies: for each argument, an exact match beats an implicit
   one (a C# implicit numeric conversion, or membership of a kind), which beats `Any`. The candidate that is no worse
   anywhere and better somewhere wins. A remaining tie is an `InvalidOperationException` that names the candidates,
   never "first declared wins".
4. **When it runs:** when the selector is parsed if the known types settle it; otherwise, when the expression is
   built, from the built argument types (the non-generic form, with candidates that constrain an untyped argument
   differently). An unknown type doesn't defer resolution where every candidate has the same constraint.
5. **The declared type comes from the table's result rule**, so it can't drift from the behaviour. The per-function
   `SelectorType` and `InitSelectorTypeToApply` overrides are gone.
6. **The build-time checks in the `Implementations` helpers stay** as a backstop until the tables are trusted, and
   for the non-generic form.
7. **Functions without a table keep count matching.** `InitializeOverloads` is now virtual with an empty default, so
   external selectors written for the old model still work.

**Considered:**
- **Constraints on the existing count-based overloads.** A smaller change per file, but it keeps the duplicated
  generic/non-generic overload code and the hand-written declared types.
- **Binding against .NET method groups** (`Math.Round`, `string.Substring`) with a binder. The smallest change for the
  math, string and date functions, but it doesn't cover LINQ, the aggregates, `COALESCE` or custom functions. It also
  works against deliberate deviations from .NET (decimals go through `double` because SQLite can't translate them).
- **Deferring** until the first same-count overload is needed. Nothing breaks today, but the Phase 4 metadata and the
  016.4 static form both wait on it.

**Consequence:** in the generic form, type errors are reported when the selector is parsed, with messages that name
the argument, the piped value or the target. Every built-in function's declared type is its built type. Writing the
tables found three more declared-type bugs: ordering (`IOrderedQueryable` vs `IOrderedEnumerable`), generic
`ElementAt` (the collection type instead of the element), and `DT_ADD*` on a nullable subject. A custom function
declares a table by overriding `Signatures`. The tables are the input for the function metadata (Phase 4).

### 020: `AGGR_COUNTDISTINCT` counts distinct non-null values; `SyntheticFunction` is removed (2026-11-25)
**Status:** accepted; implemented (2026-11-25)
**Context:** `$FN_AGGR_COUNTDISTINCT` was declared in `SelectorDefaults` with no selector behind it. The natural
expression, `x.Distinct().Count()`, counts `null` as one value. EF Core translates it in two ways. Over a group it
emits `COUNT(DISTINCT x)`, which skips `NULL`, so the same selector gave 2 in memory and 1 on SQLite for
`[null, 200]`. Over a navigation it runs a `DISTINCT` subquery. `SyntheticFunction` was an empty internal record that
nothing used, in the misspelt `SyntheticFuncions` folder.
**Decision:** `AGGR_COUNTDISTINCT` takes no arguments, applies to a collection, returns `int`, and counts **distinct
non-null values**, like SQL's `COUNT(DISTINCT x)`. When the element type can be null it builds
`x.Where(e => e != null).Distinct().Count()`. For other element types it builds `x.Distinct().Count()`. Distinct
values on a projection are written with the projection (`Reviews.Stars|$FN_AGGR_COUNTDISTINCT`), so there is no form
that takes a selector argument. `SyntheticFunction` and its folder are deleted for now, because the stub had no design
behind it. The feature itself is wanted: synthetic (composed) functions are planned for later, and the open questions
are listed in the roadmap (Phase 6, "Synthetic functions"). The stub can be recovered from `f98d935`.
Rejected: keeping .NET's semantics (null counts once). Grouped queries would disagree with memory on every provider,
because `COUNT(DISTINCT)` skipping nulls is standard SQL.
**Consequence:** in memory, `COUNTDISTINCT` differs from `Distinct().Count()` for nullable elements. That is
documented in `property-selectors.md`. Tests cover both selector forms, nullable values and references, the
per-row and grouped SQLite paths, and declared type = built type.

### 021: Selector errors are `SelectorParseException`s with a position and token (2026-11-25)
**Status:** accepted; implemented (2026-11-25)
**Context:** a bad selector string failed with whichever exception the code that noticed it threw: `ArgumentException`
for counts and literals, `InvalidOperationException` for types and unknown properties, `NotSupportedException` for
unknown functions, and a bare `Exception` from the arithmetic tokenizer and parser ("Invalid char: #", "Missing ')'").
Only the unbalanced-parenthesis check gave an index. A caller had to catch four types to turn a bad client selector
into a 400, and couldn't tell the client where the problem was. An unknown property also read differently in the two
forms: "was not found" when the generic form parsed it, and `Expression.PropertyOrField`'s "'X' is not a member of
type …" when the non-generic form built it. The review also found input that was silently accepted: leftover
arithmetic operands (`A * 2 3` dropped the `3`), unterminated strings, empty chain parts (`A||B`), empty arguments
(`f(a,,b)`) and text after a function's closing parenthesis.
**Decision:**
1. A public `SelectorParseException : ArgumentException` (the same base as .NET's `RegexParseException`), with
   `Detail`, `Selector`, `Position` (0-based, nullable) and `Token`. `Message` is `Detail` followed by
   "Position N in '…'.", and names the token when `Detail` doesn't already. No error-code enum yet; one can be added
   with the Phase 4 function metadata if UIs need it.
2. **Everything found while parsing** is a `SelectorParseException`: syntax errors, unknown functions and properties,
   literals, argument count and type errors, ambiguity and rejected pipes. Errors that helpers throw as
   `ArgumentException`, `InvalidOperationException`, `NotSupportedException` or `FormatException` are converted
   where the parser knows the position, so their wording is unchanged (`Detail`). `ArgumentNullException` and any
   other exception type are not converted, because they mean a bug, not bad input.
3. **Positions compose.** The chain split records each part's offset, function arguments record theirs, and
   arithmetic tokens carry their position. An error from a nested parse is shifted into the outer text as it passes
   through, so `$FN_MATH_MAX(Amount, $FN_MATH_SQRT(Missing))` points at `Missing` (35). Count and type errors point
   at the function.
4. **The non-generic form's unknown property** is found only when the expression is built. It is a
   `SelectorParseException` with the generic form's message, with `Selector` set to the property path, since the
   full text isn't available then.
5. The silently accepted input above is now rejected, each with its own message.
Rejected: deriving from `InvalidOperationException` (kept more existing tests passing, but a malformed string is an
argument error) or `FormatException`; converting only syntax errors (callers would still catch several types).
**Consequence:** a breaking change in exception types. Code that caught `InvalidOperationException` or
`NotSupportedException` for a bad selector must catch `SelectorParseException` (or `ArgumentException`). Tests that
compared exact messages now compare `Detail`. Build-time errors other than an unknown property, which only the
non-generic form can hit, stay `InvalidOperationException`; making them `SelectorParseException`s too is a possible
follow-up. A lone `"` used to be read as a property name and is now an unterminated string.

### 022: A pipe separates selectors; the selector after it resolves against the piped value (2026-11-25)
**Status:** accepted; implemented (2026-11-25)
**Context:** the two forms disagreed on a property after a pipe. The non-generic form built each part on the
expression so far, so `Created|$FN_DT_ADDDAYS(3)|Year` gave `x.Created.AddDays(3).Year`. The generic form created
every property part as `PropertySelector<T>`, so it looked the property up on the root type: "Property 'Year' was
not found on type 'Item'". That has been true since the generic form was added (`5e831bb`, 2026-05-24), and it has
failed outright since `GetPropertyType` started throwing (`a8c7538`, 2026-06-07). A test pinned it as a known
limitation (`ElementSelectorTests`), and another pinned its message (`SelectorParseExceptionTests`, "'Missing' was
not found on type 'Order'" for `Lines|Missing`). `.` after a function was rejected with a generic message.
`Phones|Number` failed in both forms: the generic form when it parsed the selector, the non-generic form when it
built the expression.
**Decision:**
1. **`.` navigates inside one property selector**: member access, with projection over collections and lifting
   over nullable structs. **`|` separates two selectors.** It exists because a function isn't a member of the value
   before it (`Phones|$FN_AGGR_COUNT`), and a property after a function isn't part of a path
   (`Phones|$FN_ELM_FIRST|Id`). The model was reviewed and kept. It keeps navigation, which always translates,
   apart from operations, and it needs no method-call syntax.
2. **A selector after `|` is resolved against the piped value's type, exactly as if that type were the root.** In
   the generic form, a property part is a `PropertySelector<TPiped>`, created through a delegate cached per type.
   If the piped type isn't known when the selector is parsed, the part is untyped and resolved when the expression
   is built.
3. Because of rule 2, `Address|City` is allowed and builds the same as `Address.City`. A collection piped into a
   property projects: `Phones|Number` builds `Phones.Number` in both forms. `.` is the preferred way to write
   navigation. Allowing these only turns errors into results, so nothing that worked before changes.
4. `.` after a function (`$FN_ELM_FIRST.Number`, `$FN_DT_ADDDAYS(3).Year`) is rejected with a
   `SelectorParseException` that points at the `.` and shows the piped form.
5. Function arguments are unaffected: they still resolve against the scope root (016), or against the element for
   the ordering functions (018).
Rejected: allowing `.` after a function as well. It would give two spellings for the same step and mix navigation
with operations. It could still be added at the Phase 4 grammar freeze without breaking anything. Also rejected:
refusing a member after a function, which would remove chains that worked in the non-generic form.
**Consequence:** `Lines|Missing` now reports the element type ("not found on type 'Line'"). The two tests that
pinned the old behaviour were rewritten. In the non-generic form, an unknown property in a later part is reported
when the expression is built, with a position relative to that part (021.4).
