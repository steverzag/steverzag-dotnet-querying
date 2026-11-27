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
    - *Done (2026-11-26, decision 035):* the piped form comes from `FunctionCatalog.After(type)`, not from the tree.
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
   external selectors written for the old model still work. *Superseded by 034 (2026-11-26): every function declares a
   table, and the count-matching model is removed.*

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

### 023: At the start of a part, a collection's own members come before its elements (2026-11-25)
**Status:** accepted; implemented (2026-11-25)
**Context:** when a part started on a collection, the generic type resolver (`GetPropertyType`) always went to the
element type first. The expression builder (`CreateMemberAccessRec`) tried the collection's own member first and
only then projected. For a grouping this meant `PropertySelector<IGrouping<K, T>>` couldn't resolve `Key` ("not
found on type 'Product'"), so QueryShaper's grouped `ApplySelect` used the non-generic form as a workaround and
lost errors at parse time. With 022 a piped collection became reachable, and `Names|Length` on a `string[]`
declared `IEnumerable<int>` but built the array's `int` length.
**Decision:** the resolver follows the builder. At the **start of a part** (the root, or a piped value), a member is
looked up on the value itself first. If it isn't there and the value is a collection, it's looked up on the
elements, which projects. So `Key` on a grouping is its key, `Quantity` projects the rows, `Names|Length` is the
array length and `Phones|Count` is the list's `Count`. **Inside a dotted path**, a collection still projects:
`Names.Length` is each name's length. Grouped `ApplySelect` parses with `PropertySelector<IGrouping<GroupByKeys, T>>`.
Rejected: element-first everywhere. It would mean changing the builder, which would break `Key` in the non-generic
form and every grouped select. Also rejected: member-first inside dotted paths, which would change what
`Names.Length` means today.
**Consequence:** 022's "`Phones|Number` ≡ `Phones.Number`" holds only when the collection itself has no member with
that name. When both have one (`Length`, `Count`), the piped and dotted forms differ, as documented in
`property-selectors.md` ("Dot and pipe"). Nothing that worked changes: the generic form gains `Key` and collection
members, and one wrong declared type is corrected. Grouped select errors now come from the generic parser, with
positions in the whole selector.

### 024: Decimal rounding is exact; SQLite gets an opt-in fallback through `double` (2026-11-25)
**Status:** accepted; implemented (2026-11-25)
**Context:** `ROUND`, `CEILING`, `FLOOR`, `TRUNCATE`, `MAX` and `MIN` on a `decimal` went through `double` (decided
2026-11-24 while implementing 016), because EF's SQLite provider can't translate `Math` over `decimal`. That loses
precision for money (`Math.Round(0.5015m, 3)` is 0.502, through `double` 0.501) and declares `double`. A provider
check (EF Core 9.0.10, Npgsql 9.0.4; table in the roadmap) found more. The `double` path doesn't translate
`Round(x, digits)` on PostgreSQL, so `Price|$FN_MATH_ROUND(2)` failed there. The `decimal` rounding functions
translate exactly on SQL Server and PostgreSQL. `Math.Max`/`Min` over `decimal` on SQLite compare as text and return
wrong results with no error (`max(9, 10)` is 9). And `Math.Max`/`Min` don't translate on SQL Server below
compatibility level 160, whatever the type.
**Decision:**
1. On a `decimal` (or `decimal?`) operand, `ROUND`, `CEILING`, `FLOOR` and `TRUNCATE` use `Math`'s `decimal` overloads
   and declare `decimal`. This is done in `ForOperandType`, which already picks the overload for the operand type,
   so the signature tables don't change. Other operand types are unchanged.
2. `MAX`/`MIN` on a `decimal` stay on `double`. Changing them would only gain the declared type, and it would add
   silently wrong results on SQLite. A conditional form that would fix them everywhere is a roadmap follow-up.
3. `Steverzag.QueryShaper.EntityFrameworkCore` adds `UseDecimalMathAsDouble()`, an opt-in `IQueryExpressionInterceptor`
   for providers that can't translate the `decimal` overloads (SQLite). It rewrites
   `Math.Round/Ceiling/Floor/Truncate(decimal…)` to `(decimal)Math.X((double)…)` before EF compiles the query. The core
   stays provider-agnostic (006), and it needs no configuration hook, so it doesn't wait for D4.
Rejected: a global or per-call setting in the core (needs D4, and every caller would have to know their provider);
keeping `double` as the default (wrong values on the providers that can do better, and broken on PostgreSQL);
moving `MAX`/`MIN` to `decimal` now (silent wrong results on SQLite).
**Consequence:** a breaking change in declared and built types. `Price|$FN_MATH_ROUND(2)` is `decimal` instead of
`double`, so a mapping into a `double` destination now converts. On SQLite without the fallback, these four functions
over `decimal` fail with "could not be translated", which is an error rather than a wrong value. With the fallback
they have `double` precision on SQLite, as before.

### 025: `MAX`/`MIN` are a conditional that ignores nulls (2026-11-25)
**Status:** accepted; implemented (2026-11-25). Supersedes 024.2.
**Context:** `MATH_MAX`/`MATH_MIN` called `Math.Max`/`Min`. EF translates those to `GREATEST`/`LEAST`, which SQL
Server only has from 2022 (compatibility level 160), so they didn't translate on older SQL Server whatever the type.
SQL Server's `MAX` is an aggregate over rows, not a two-argument function. Over `decimal` they went through `double`
(024.2), because SQLite's `decimal` `max()` compares text and returns wrong values with no error. Nulls behaved
differently everywhere: a nullable operand was converted to non-nullable (throws in memory), PostgreSQL's
`GREATEST` ignores nulls, and SQLite's `max()` returns null.
**Decision:**
1. `MAX`/`MIN` are built as a conditional, `a >= b ? a : b` (`<=` for `MIN`), over the operands widened to their
   common numeric type as in arithmetic. Providers translate it as `CASE WHEN`, and on SQLite EF compares decimals with
   `ef_compare`, so decimals are exact everywhere.
2. **Nulls are ignored:** a null operand gives the other one, and the result is null only when both are. This matches
   `GREATEST`/`LEAST` on SQL Server and PostgreSQL and the `AGGR_MAX`/`AGGR_MIN` aggregates. The null checks are only
   built for a nullable operand.
3. The result type is the common type, nullable only when both operand types are nullable.
Rejected: keeping `Math.Max` and documenting SQL Server 2022+ (excludes a large share of SQL Server installs, and
doesn't fix decimals); null if either is null, like arithmetic (less useful for "the later of two optional dates",
and still reachable with `COALESCE`).
**Consequence:** breaking changes in types and values. `MAX` on a `decimal` is `decimal` instead of `double`,
`MAX(int?, int)` is `int`, and a null operand gives the other operand instead of throwing (in memory) or depending on
the provider. Each operand appears twice in the SQL (three times when nullable), so an expensive operand such as a
subquery may be evaluated more than once. In memory, `NaN` isn't propagated as `Math.Max` does it.

### 026: A parenthesized group can start a chain; `|` binds tighter than arithmetic (2026-11-25)
**Status:** accepted; implemented (2026-11-25).
**Context:** `(Age + 1)|$FN_MATH_ROUND` failed in both forms, because the `(Age + 1)` part was read as a property
name. The informal grammar already gave `|` a tighter binding than `+ - * /` (arithmetic operands are paths,
literals or chains, so `A + B|$FN_X` is `A + (B|$FN_X)`), but it said nothing about a group in front of a pipe. The
static form (`$FN_MATH_ROUND(Age + 1)`) covers functions that have one, but dependent functions (`COALESCE`) and a
property after the value (`(A + B)|…|Member`) have no other way to receive a computed value.
**Decision:**
1. A parenthesized group is a primary, like a path or a literal, and a primary can start a chain:
   `(Age + 1)|$FN_MATH_ROUND` ≡ `$FN_MATH_ROUND(Age + 1)`, `(Price * 3)|$FN_MATH_ROUND(2)` ≡ `$FN_MATH_ROUND(Price * 3, 2)`.
2. Precedence is unchanged: `|` binds tighter than every arithmetic operator, so `A + B|$FN_X` stays `A + (B|$FN_X)`.
   Groups nest: `((A + B)|$FN_MATH_ROUND) * 2`.
3. The parts after the group follow decision 022: each one resolves against the type of the value piped into it.
4. Both forms (generic and non-generic) accept it, with the same declared and built type.
Rejected: static form only (dependent functions and members after the value would have no way to receive a
computed value, and the error would only point to a workaround).
**Consequence:** additive; nothing that parsed before changes meaning. A selector that starts with `(` is now always
arithmetic, so one wrapped entirely in parentheses (`((Amount + 2) * 2)`), which used to fail as a property name, now
works. An arithmetic segment followed by a pipe prints in parentheses, so `ToString()` round-trips. To freeze in the
Phase 4 grammar spec.

### 027: Arithmetic stays numeric-only; date and duration math goes through functions (2026-11-25)
**Status:** accepted.
**Context:** arithmetic accepts only numeric operands (`GetWiderNumericType` throws "Type DateTime is not numeric").
The open question was whether to follow .NET's operator rules for dates and durations (`End - Start` → `TimeSpan`,
`Start + Duration`, `Duration * 2`) before the Phase 4 grammar freeze. `TimeSpan` has no support yet (no literal, no
parts), and translation varies by provider: without `EF.Functions` (006), plain date subtraction doesn't translate
everywhere, and the grammar has no way to say so.
**Decision:**
1. For `0.1.0-preview`, `+ - * /` and unary `-` accept numeric operands only. A date or duration operand is
   rejected, as today.
2. Date math goes through functions in the date catalog: differences (`DT_DIFF*`), and an add that takes a
   `TimeSpan` once that sub-family exists. Each function documents how providers translate it.
3. Allowing date and duration operands later only relaxes a rule that throws today, so it can be revisited after the
   preview as an additive change, once `TimeSpan` support and provider checks are in place.
Rejected for now: operators with .NET's rules (depends on `TimeSpan` support that doesn't exist, and would freeze
translation behaviour we haven't checked; taking operators back after release would be breaking).
**Consequence:** no code change. The grammar spec states that arithmetic is numeric-only. The error for a date
operand could point to the date functions once `DT_DIFF*` exists.

### 028: Client filter values are query parameters; time-typed lists stay inlined (2026-11-25)
**Status:** accepted; implemented (2026-11-25).
**Context:** filters and the base search embedded each client value with `Expression.Constant`. EF Core makes
constants part of the query-cache key and inlines them into the SQL, so every distinct value compiled a new query
and the cache grew with each one. A value list was inlined element by element, and `ContainsAny` had one shape per
list length.
**Decision:**
1. Every value that comes from the client is read from a closure-style holder (`FilterValue<T>.Value`), which EF Core
   sends as a SQL parameter. Values the filter fixes itself (`0`, `null`, `""`, `true`/`false` in `IsEmpty` and
   friends) stay constants.
2. A value list (`In`, `NotIn`, `ContainsAny`, `ContainsAll`) is one parameter, which EF Core 8+ sends as JSON
   (`json_each` on SQLite, `OPENJSON` on SQL Server). `ContainsAny` is built as `collection.Any(y => values.Contains(y))`.
3. **Exception:** a list whose element type is `TimeOnly`, `TimeSpan` or `DateTimeOffset` stays inlined. On SQLite,
   EF Core writes `TimeOnly`/`TimeSpan` JSON elements in a different text format from the column (`0:00:00` vs
   `00:00:00`), so a parameterized `In` silently matches nothing. `DateTimeOffset` isn't in the test model, so it
   can't be checked and stays on the old, correct path.
**Consequence:** one compiled query per filter shape instead of per value. A list compiles once whatever its length,
except for the three inlined element types. The SQL now shows `@__Value_N` instead of the literal, which matters
for anyone reading logs. Revisit the exception when EF Core fixes the provider, or when SQL Server/PostgreSQL get
their own test run.

### 029: `ValueResult2` becomes `FilterValue`; an invalid `Guid` string is rejected (2026-11-25)
**Status:** accepted; implemented (2026-11-25).
**Context:** `ValueResult2` (file `Utils/SearchValue.cs`) and `ResultValueKind` were temporary names, and
`ConditionNode.SearchValue` used "search" for a filter value while the base search uses it for its text. The type also
reported only some of its kinds: every integer was `Integer` and `float` was `Double`, so `Byte`, `Short`, `Long` and
`Float` were never produced. A `Guid` cast from an unparseable string returned `Guid.Empty` (or null for `Guid?`).
**Decision:**
1. `ValueResult2` → `FilterValue`, `ResultValueKind` → `FilterValueKind`, both in `Steverzag.QueryShaper.Filtering`.
   `ConditionNode.SearchValue` → `ConditionNode.Value`. "Search" now means only the base search.
2. `FilterValueKind` lists only the kinds `FilterValue` produces: `Integer` for every integer type, `Double` for `float`
   and `double`. Consumers only need the category; the exact type is converted against the property (013).
3. An unparseable string cast to `Guid` or `Guid?` throws `FormatException`; a filter reports it as
   `FilterConditionAndValueNotSupportedException`. A valid string still works.
Rejected: `SearchValue` (keeps "search" ambiguous); `FilterOperand` (precise, but less familiar); reporting the four
extra kinds (nothing reads them).
**Consequence:** breaking renames before the first release. `FilterValueKind`'s numeric values change. A filter that
used to match `Guid.Empty` or null on a bad `Guid` string now throws. The JSON contract for `QueryRequest` will bind a
condition's value as `value`.

### 030: Lexical rules for the grammar freeze: limits, case, `$`, escapes, the root (2026-11-25)
**Status:** accepted; implemented (1–5); versioning settled by 032 (roadmap Phase 4, "Grammar freeze: lexical rules").
**Context:** before freezing the grammar, its open points were checked against the parser and probed with about
120 selectors through both forms. Most rules were already settled by the code. Five weren't, and the probe found that
a long or deeply nested selector overflows the stack: 2,000 nested parentheses (4 KB), 1,000 nested functions
(15 KB) or a flat sum of 3,000 terms (18 KB) crash the process, and .NET can't catch a stack overflow.
**Decision:**
1. **Limits, checked when the selector is parsed:** a maximum length (default 2,000 characters) and a maximum depth of
   the selector tree (default 32), configurable, with the defaults in PropertySelectors itself so they hold without
   QueryShaper. Depth counts the tree, not the parentheses, so a flat sum of n terms (a left-deep tree) has depth n.
   The length is checked before anything else is scanned. Exceeding either is a `SelectorParseException`.
2. **Property names and function tokens are case-sensitive**, in both forms. Today property lookup ignores case
   (`age` finds `Age`), and two properties that differ only by case throw a raw `AmbiguousMatchException`.
3. **`$` is reserved.** Anything starting with `$` that isn't a registered function is "Unknown function '…'", never
   a property lookup. This also covers a wrongly cased token (`$fn_str_trim`) and the removed `$VAL_` forms.
4. **A property name can be escaped**, so a property whose name is also a literal (`True`, `false`) is reachable.
   Proposed form: a leading `@`, as with C# verbatim identifiers (`@True`). The arithmetic tokenizer already
   accepts `@` at the start of an operand. To confirm when implemented.
5. **Function arguments always resolve against the scope root, even when the chain starts with a literal or a
   function without a subject.** Today `5|$FN_MATH_MAX(Age)`, `"x"|$FN_STR_CONCAT(Name)` and
   `$FN_DT_NOW|$FN_DT_ADDDAYS(Age)` look the argument up on the piped value's type, because the root is searched for
   inside the piped expression and a constant has none. Fix by passing the root down instead of searching for it;
   if that turns out not to be possible, document it as a limit of the spec.
**Consequence:** breaking for any caller that relied on case-insensitive property names, and for selectors longer or
deeper than the limits. Still open for the freeze: what "frozen" promises for `0.1.0-preview` (versioning).
**Implementation note (2026-11-25), 030.1:** implemented as `SelectorLimits` (`MaxLength`, `MaxDepth`). Three details
were settled while building it:
- **Parentheses count toward depth**, although they add no node to the tree. Otherwise nesting could hide in redundant
  parentheses, which only the parser sees. The parentheses the tokenizer adds around a negated operand don't count.
- **`MaxDepth` can't be set above 256** (`MaxDepthCeiling`). An error found deep down has to travel back up through
  every level, and that has to fit on the stack too.
- **A limit error carries the whole selector and no position**, instead of being re-wrapped at each level with an
  offset. The text at fault is the selector as a whole.

**Implementation note (2026-11-25), 030.3:** both chain loops (`PropertySelector.Chain`, `PropertySelector<T>.Chain`)
now send any part that starts with `SelectorDefaults.ComplexSelectorStart` (`$`) to the registry, not only `$FN_`, so
an unregistered token is `UnknownFunction` wherever it appears. Two details:
- **A wrongly cased token names the registered one** ("Function names are case-sensitive: '$FN_STR_TRIM'."), found
  with an ordinal-ignore-case scan of the registry that runs only on the error path.
- **`SelectorDefaults.ArithmeticStart` and `ArithmeticOperation` are now `internal`.** `$ARITH_OPERATION` is only the
  `PlainValue` sentinel of an arithmetic node; nothing parses or compares it, and it can no longer be written in a
  selector. Breaking only for code that read the constants.

**Implementation note (2026-11-25), 030.2:** both forms now look a member up through one resolver,
`TypesExtensions.FindProperty`: public instance, non-indexed properties, exact (ordinal) name, walking from the type
up its base types (and, for an interface, its inherited interfaces) and taking the first match. The generic form used
it already, with `IgnoreCase`; the non-generic build (`ExpressionUtils.CreateMemberAccessRec`) used
`Expression.Property(expression, name)`, which ignores case too. Three details:
- **The two forms disagreed on visibility.** `Expression.Property(expression, name)` falls back to non-public
  properties, so an `internal` property was reachable in the non-generic form only. Both forms now reject it.
- **Walking the hierarchy fixes `new`.** A property hidden with `new` (same name in a base and a derived class) made
  `GetProperty` and `Expression.Property` throw a raw `AmbiguousMatchException`; the most derived one wins now, as in
  C#. Two properties that differ only by case did the same, and are now simply two properties.
- **A wrongly cased name names the real one** ("Property names are case-sensitive: 'Age'."), like a wrongly cased
  function token (030.3). The ignore-case scan runs only on the error path, in `TypesExtensions.PropertyNotFound`.

**Implementation note (2026-11-25), 030.4:** the `@` form is confirmed (`SelectorDefaults.PropertyNameEscape`).
Four details:
- **The escape stays in `PlainValue` and is removed where a name is looked up.** Both lookups (`GetPropertyType`
  in the generic form, `CreateMemberAccessRec` in the non-generic build) go through
  `TypesExtensions.UnescapePropertyName`. So `ToString()` round-trips with no extra code, and error positions point
  at the name itself, one past the `@`. The parser needs no change: a part starting with `@` is never a literal and
  never a function, so it already reaches the property path.
- **Per segment, not per part.** `Inner.@True` and `@Inner.@True` are allowed, as in C# (`a.@class`). Only the first
  segment of a part can collide with a literal, but allowing it everywhere keeps the rule one sentence.
- **Allowed on any name.** `@Age` is `Age`. Rejecting a "needless" escape would make callers know the literal list.
- **Known gap:** a selector built in code with a literal-looking name (`new PropertySelector<T>("True")`) prints
  `True`, which parses back as the literal. `ToString()` doesn't add the escape, because the base `SegmentToString`
  is shared with the value selectors. Parsed selectors aren't affected.

### 031: Function arguments resolve against the scope root; `:` is reserved for aliases (2026-11-25)
**Status:** accepted. Part 1 confirms what is implemented; part 2 implemented (2026-11-25); part 3 is an experiment for later.
**Context:** after the root-lost fix (030.5), the question was what an argument should resolve against when the chain
has moved into something else: in `Phones|$FN_ELM_FIRST|Id|$FN_MATH_MIN(Id)` on `AppPerson`, is the argument's `Id`
the phone's or the person's? The generic chain carries two types: the piped value's type (`currentIterationType`),
which is the function's subject, the type of the piped placeholder and what a property part after `|` is looked up on
(022); and the root type (`T`), which the arguments are typed against (018). Three options were weighed: (A)
arguments against the root, (B) arguments against the piped value, (C) A plus an explicit way to name a value from
the chain.
**Decision:**
1. **Keep A.** The chain moves the subject; every function argument resolves against the scope root, which is the
   query's row type, wherever the function sits in the chain. The ordering functions are the exception: their arguments
   resolve against the collection's element (018, `ArgumentScope.TargetElement`). So in the example above the
   argument's `Id` is `AppPerson.Id`, and the phone's `Id` is reached through the chain (`…|Id|$FN_MATH_MIN(5)`).
   Why: it keeps D7's equivalence `X|$FN_F(a)` ≡ `$FN_F(X, a)` (in the static form `a` can only mean the root's `a`),
   and it serves the common case of combining a value with another column of the row (`Created|$FN_DT_ADDDAYS(TrialDays)`).
   B was rejected: it breaks that equivalence and makes the column case impossible.
2. **Reserve `:`** outside quoted strings, before the grammar freeze, so that aliases can be added later without
   changing what any valid selector means. Today an unquoted `:` has no meaning (it is read as part of a property name
   and fails as "not found").
3. **Experiment for later: aliases** (option C). A chain step could be named and an argument could start from it, for
   example `Phones|$FN_ELM_FIRST:a|Id|$FN_MATH_MIN(:a|Id - 1)`: `:a` after a step names its value, and `:a` at the start
   of an argument refers to it. To settle when it's tried: the exact syntax, the scope of a name (inside the chain only,
   or also in nested arguments), name rules, interaction with the ordering element scope, how the declared type flows
   to the generic form, `ToString()`, and whether EF translates the repeated subexpression well (an alias is
   re-evaluated, not stored, unless it's built with a `let`-style projection).
**Consequence:** no behaviour change from part 1. Part 2 adds one rejection to the freeze work. The unused public
`SelectorDefaults.ArithmeticOperationEncloser` (`"::"`) should go in the public-API audit, so it isn't mistaken for a
use of the reserved character.

### 032: What the grammar freeze promises: versioning for `0.1.0-preview` (2026-11-25)
**Status:** accepted (2026-11-25). Closes the versioning point left open by 030.
**Context:** the lexical rules (030) are implemented, and the grammar spec is to be written next. "Frozen" needs a
definition before the spec is written, so the spec can say what it guarantees. Selectors are a wire format: they're
stored in saved reports and sent by clients that upgrade on their own schedule, so a change in what a selector
*means* is worse than a C# compile error, because nothing catches it.
**Decision:**
1. **SemVer per package, with the 0.x convention made explicit.** PropertySelectors and QueryShaper version
   independently (D6). While the major version is 0, a release that breaks anything bumps the **minor** (`0.1` → `0.2`),
   and an additive or fix-only release bumps the **patch**. Pre-release builds (`0.1.0-preview.N`) may break between
   each other, but every break is listed in the CHANGELOG under "Breaking".
2. **The grammar contract is the selector text and what it builds.** For a selector that parses today, these are the
   contract: whether it parses, the expression's result value, the declared type (generic form) and the built type,
   and `ToString()` round-tripping. Changing any of them is breaking. So are: removing or renaming a function token,
   changing a function's argument meaning or order, lowering a limit default (`SelectorLimits`), and changing the
   exception type or `Position`/`Token` of an error.
3. **Additive, so allowed in a patch:**
   - a new built-in function, a new overload, or a new literal form, **as long as every selector that already parses
     resolves exactly as before** (an overload that makes an existing call ambiguous or pick a different overload is
     breaking, 019);
   - giving meaning to text that's rejected today. This is what `$` (030.3), `:` (031.2) and `@` (030.4) are reserved
     for, and why the reserved forms reject instead of falling through;
   - raising a limit default;
   - a different SQL shape for the same result.
4. **Not part of the contract:** the wording of error messages (`Detail`/`Message`), the exact shape of the
   expression tree (as long as value, types and provider translation hold), and anything `internal`.
5. **Third-party function tokens.** A new built-in token can collide with one an application registered, and the
   registry throws on a collision at start-up. To keep this additive, built-ins never use a vendor-scoped name: the
   built-in families are `AGGR_`, `ELM_`, `ORDER_`/`THEN_ORDER_`, `MATH_`, `STR_`, `DT_` and the bare `COALESCE`, and
   applications are told to use `$FN_<VENDOR>_…` (already the advice on `SelectorRegistry`). A new built-in family
   prefix is a breaking change.
6. **The grammar spec is versioned with the package.** The "Grammar" section of `property-selectors.md` states the
   version it describes, and the CHANGELOG lists grammar changes separately from C# API changes.
7. **QueryShaper pins a PropertySelectors minor.** It depends on `[0.1.0, 0.2.0)`, since a 0.x minor may break.
**Consequence:** the reserved-prefix rules become load-bearing: anything that falls through to a property lookup
today can't be given meaning later without a breaking release. The public-API audit (Phase 4) should run before
`0.1.0-preview`, because whatever is public then is under rule 2 from 0.1 onwards.

### 033: A literal can't follow `|` (2026-11-25)
**Status:** accepted and implemented (2026-11-25).
**Context:** writing the grammar spec (grammar 0.1) showed that a literal after a pipe was accepted in both forms and
silently replaced the piped value: `Age|5` built `5`, `Name|"x"` built `"x"`. Nothing used it, and a selector like
that is almost certainly a mistake (a missing function, or a value meant as an argument). Under 032, rejecting
accepted text after `0.1.0-preview` would be breaking, so it had to be settled before the release.
**Decision:** a literal (number, string or boolean) can only start a chain. After `|` it's a `SelectorParseException`,
"A literal can't follow '|': it would discard the value before it.", positioned at the literal. This holds wherever
a chain appears: at the top level, after a group (`(Age + 1)|5`), as an arithmetic operand (`Age + Name|"x"`) and as an
argument (`$FN_MATH_MAX(Age|2, 1)`). A literal at the head of a chain keeps working (`5|$FN_MATH_SQRT`), and a property
named like a literal is reached with `@` (`Inner|@True`, 030.4).
**Implementation:** both chain loops (`PropertySelector.Chain`, `PropertySelector<T>.Chain`) throw
`PropertySelectorUtils.LiteralAfterPipe` when a part after the first classifies as a literal. Group tails go through
the same loops, so they need nothing extra. Tests: 36 in `LiteralAfterPipeTests`, both forms (all 22 rejections
failed on the old code; the 14 allowed cases pass on both).
**Consequence:** breaking only for selectors that relied on the replacement, which no test or doc example did.
Giving a literal after `|` a meaning later stays possible as an additive change.

### 034: A function's signatures are a static contract, and the table is the only model (2026-11-26)
**Status:** accepted and implemented (2026-11-26).
**Context:** the function metadata for UIs (Phase 4) has to list each function's overloads without creating it. The
table was a `protected virtual` instance property, and the parameterless constructor runs overload resolution, so it
throws for a function like `MATH_ROUND` that needs arguments. The built-ins kept the table in an `internal static
Table`, a convention that external selectors couldn't follow. The count-matching model from before 019
(`Overloads`, `InitializeOverloads`, `FunctionPropertySelectorOverload`) was still public, but no built-in used it.
**Decision:**
1. A public `IFunctionSignatures` interface with one `static abstract IReadOnlyList<FunctionSignature> Signatures`.
   Both records of a function implement it and return the same table.
2. The base records read the table through `FunctionSignatures.Of(GetType())`, cached per closed type. A selector
   without the interface throws `InvalidOperationException` when constructed.
3. `SelectorRegistry.Register` (and so `RegisterAssembly`) rejects a type without the interface with an
   `ArgumentException`, before anything is registered.
4. The count-matching model is removed: `Overloads`, `InitializeOverloads()` and `FunctionPropertySelectorOverload`.
   This supersedes 019.7. `SelectorType` always comes from the table's result rule.
**Considered:**
- **A static `Signatures` found by naming convention.** No compile-time check, and a typo would only show at runtime.
- **Creating the selector without running its constructor** (`RuntimeHelpers.GetUninitializedObject`) and reading the
  virtual property. It works, but it relies on the property not touching instance state.
- **Keeping the old model for external selectors.** Nothing outside the lab uses it before `0.1.0-preview`, and every
  function in the catalog would then need an "unknown signatures" case.
**Consequence:** breaking for an external selector written against the old model, which is acceptable before the first
release. The function catalog (Phase 4) can read every registered function's table from its type alone. Tests: 70 in
`FunctionSignaturesTests` (one table shared by both forms for every built-in, readable without an instance, the
resolved `Signature` comes from it, and rejection at registration and construction). The `Overloads` asserts in the
element and ordering tests now check `Signature`.

### 035: The function catalog, function categories, and exact targets for `DT_ADD*` and `THEN_ORDER_*` (2026-11-26)
**Status:** accepted and implemented (2026-11-26).
**Context:** the Phase 4 item "Function metadata for UIs" (design agreed 2026-11-26). The property tree can't hold
functions: they apply to a value's type, not to a property path, and a chain has no fixed length. Building the
catalog showed that two tables accepted more than the functions support. Each `DT_ADD*` took any date-like type, so
`Day|$FN_DT_ADDHOURS(1)` on a `DateOnly` and `$FN_DT_ADDSECONDS(OpensAt, 30)` on a `TimeOnly` failed only when built,
with an `InvalidOperationException`. `THEN_ORDER_*` took any collection, so `Lines|$FN_THEN_ORDER_ASC(Amount)` failed
when built with a raw `ArgumentException` from `Expression.Call`. A catalog read from those tables would offer them.
**Decision:**
1. **`FunctionCatalog`** (namespace `MetaData`), a static class over the registry:
   - `All`: every registered function, ordered by token. `Find(token)`: one, or `null` (exact token, case-sensitive).
   - A `FunctionDescriptor` has the token, category, piped form and hint, argument scope, which forms are registered,
     and the signatures. The signatures are the non-generic record's table, or the generic one closed over `object`
     when only that form is registered.
   - `After(Type)`: the overloads that can follow a value of that type with `|`, as `FunctionMatch`es. Each match has
     the arguments still to write (without the piped one) and the result type from the table's rule, given the piped
     type and unknown other arguments; `null` when it depends on them. The rules are the parser's: the target must
     match; for `PipedForm.FirstArgument` argument 0 must match too; `PipedForm.Rejected` is never listed; an overload
     whose result rule rejects the type is left out.
2. **`FunctionCategory`** on `[SelectorToken]` (`Category = …`): `Aggregation`, `Element`, `Ordering`, `Math`,
   `String`, `Date`, `Null`, and `Other` as the default for custom functions. Every built-in sets one.
3. **Each `DT_ADD*` accepts only the date types that have that method**, found by reflection the same way the call
   is built. Its description lists them, so the parse error says what was expected ("must be a DateTime or
   DateTimeOffset, not a TimeOnly").
. **`ArgumentConstraint.Accepts(Type)`** (added the same day): true unless the constraint rejects the type, so a UI
   can pick the properties that fit an argument. Literal-only constraints accept no type (`IsLiteral` says why).
   `FunctionCatalog.After` uses it. `Match` stays public for now; whether it and `ConstraintMatch` become internal
   is left to the public-API audit. Tests: 33 in `ArgumentConstraintAcceptsTests`.
**Considered:**
- **Functions as nodes of `SelectorsMetaDataTree`.** The tree would need every function under every node, and again
  under each result, without end.
- **Leaving the tables as they were and filtering in the catalog.** The catalog would then hold its own copy of the
  rules, and the parse errors would stay late and raw.
**Consequence:** 3 and 4 move errors from build time to parse time and make them `SelectorParseException`s. No selector
that used to work is rejected. Tests: 43 in `FunctionCatalogTests`, including a cross-check that every match for
twelve subjects (numbers, nullables, strings, dates, collections, an ordered collection) parses with sample arguments
and declares the type the catalog reported. The two `THEN_ORDER_*` tests and the `DT_ADDSECONDS`-on-`TimeOnly` test
that pinned the build-time error now expect the parse-time one, in both forms.

### 036: The property tree follows the parser, flags collections, and carries display names (2026-11-26)
**Status:** accepted and implemented (2026-11-26).
**Context:** the last step of "Function metadata for UIs". A probe of `PropertiesExplorer<T>` showed that the tree
didn't list what a selector can reach. Under a `List<Phone>` it listed `Capacity`, `Count` and the indexer `Item`
(and `Item`'s properties); under an `int[]`, `Length`, `Rank` and `SyncRoot`. None of these parse: after the first
segment a path projects over the elements (`Phones.Count` is "Property 'Count' was not found on type 'Phone'").
Under a `DateTime?` it listed `HasValue` and `Value`, with the date parts under `Value`, while `Deleted.Year` is
the form that works. Each node's `SelectorType` was the property's own type, but the parser lifts and projects:
`Deleted.Year` declares `int?`, `Phones.Number` `IEnumerable<string>`, `Orders.Lines.Notes` `IEnumerable<string>`.
A property whose type was already on the path (`Friends: List<Person>` on `Person`, `Deleted.Date`) was dropped.
Nodes had no path, and `System.Type` doesn't serialize.
**Decision:**
1. Children follow the parser's lookup: a collection node lists its element's properties, a nullable node its
   underlying type's. Indexers are skipped. Fields stay out (properties only, as for the non-generic form).
2. Each node gets its `Path`, the selector text. A name that reads as a literal (`true`/`false` in any case) gets the
   `@` escape (030.4).
3. `SelectorType` is the type the path declares, computed by `TypesExtensions.GetPropertyType`, the same code the
   parser uses, so the two can't drift.
4. `IsCollection` and `ElementType` describe the node's own type, so `Phones.Number` is a collection of `string`.
5. The cycle guard ends the walk instead of hiding the property: a type already on the path is listed as a leaf.
6. Display names: a public `TypeDisplay.Name(Type)` writes C#-style names with keyword aliases (`int?`,
   `List<Phone>`, `string[,]`). Nodes have `TypeName` and `ElementTypeName`, and `FunctionMatch` has
   `ResultTypeName`. No built-in JSON shape: a caller maps to its own DTO. Error messages keep .NET names
   (`String`, `Int32`), since they're read by developers.
**Considered:**
- **Keeping the property's own type on each node and adding the declared type beside it.** Two types per node, and
  `After` needs the declared one anyway.
- **Using .NET names (`Int32`) for display too**, to match the error messages. Names a UI shows its users should
  read like the C# that declared the properties.
**Consequence:** the tree's shape and types change for collections and nullable values, which nothing outside the
tests used. Tests: 46 in `PropertiesExplorerTests` and `TypeDisplayTests`, including a cross-check that every
node's path parses (generic form) and builds (both forms) to the node's type, and that every collection node is
offered `AGGR_COUNT` by the catalog.

### 037: Target frameworks and the production versioning model (2026-11-26)
**Status:** accepted (2026-11-26). Settles D2.
**Context:** every project targets `net9.0` (`Directory.Build.props`), with EF Core and DI abstractions 9.x. The
roadmap recommended `net8.0` + `net9.0`. Both reached end of support on 2026-11-10; .NET 10 is the current LTS.
EF Core releases a major per .NET release, but its target framework doesn't always match it (EF Core 9 targets
`net8.0`), and `Microsoft.Extensions.*` keep the yearly numbering while multi-targeting.
**Decision:**
1. **`0.1.0-preview` ships on `net9.0`**, as the projects are today. No retargeting before the preview.
2. **From 1.0, a hybrid model:**
   - `Steverzag.QueryShaper.EntityFrameworkCore`: its major follows EF Core's major (EF Core 11 → 11.x), and it
     targets what that EF Core major targets. This is where the coupling is real: what translates changes per EF
     major.
   - `Steverzag.PropertySelectors` and `Steverzag.QueryShaper`: independent SemVer (032), multi-targeting every
     supported .NET version, adding and dropping targets on Microsoft's support schedule. Expression trees and
     `IQueryable` change little between releases, so a yearly major would announce changes that didn't happen.
   - Only the newest major of each package receives fixes.
3. The mapping between the EF add-on's major and the QueryShaper/PropertySelectors versions it pins (032) is
   written down per release, in the add-on's CHANGELOG.
**Considered:**
- **`net8.0` + `net9.0`** (the old recommendation). Both are out of support.
- **`net10.0` only for the preview.** Clean, but a retarget (and EF Core 10 for the lab migrations) isn't needed to
  ship a preview.
- **Every package's major follows .NET.** Clear support story, but it forces a yearly major on PropertySelectors
  with no breaks, makes a mid-year break wait for November or break the scheme, and conflicts with 032's
  "a break bumps the version".
- **Servicing several majors at once, as EF does.** Too much branch work for a solo maintainer.
**Consequence:** the preview runs on an out-of-support runtime; the README says so and names 1.0 as the point the
targets move. Phase 6 carries the move to the hybrid model.

### 038: No ambient configuration registry; an injected entry point is the direction (2026-11-26)
**Status:** accepted (2026-11-26). Settles D4.
**Context:** `AddQueryShaper<T>()` registers the `Mapper` and `BaseSearchConfiguration` in the container and also
copies them into the static `QueryShaperServices`, which `ApplySelect` and `ApplyBaseFilter` fall back to when no
instance is passed. The aim was an AutoMapper-like call site (configure once, call without passing it). The static
follows whichever container called `AddQueryShaper` last (`ConfigurationTests.TheStaticFallback_FollowsTheLastServiceCollection`),
so parallel test hosts, several hosts in one process and per-tenant configuration silently share or swap it; a
"nothing registered" test can't be written reliably; the plain `Dictionary` is written while other hosts read it.
The configuration itself is a snapshot that will be redesigned.
**Decision:**
1. **For `0.1.0-preview`: remove `QueryShaperServices`.** The `IMapper` parameter of `ApplySelect` and the
   `IBaseSearchConfiguration` parameter of `ApplyBaseFilter` become required. `AddQueryShaper` keeps registering
   both in the container, so callers inject them.
2. **Direction: one injected entry point, `IQueryShaper`**, carrying the mapper, the search configuration and future
   settings (guard rails, And/Or evaluation order). It's designed with the configuration redesign and the
   `QueryRequest` `Apply(request)` item, not before.
3. **A static default stays possible later, as an explicit opt-in** (e.g. `UseQueryShaperDefaults()`, Mapster's
   global-settings model), added only on demand. Adding it doesn't break anyone; removing a fallback after release
   would.
**Considered:**
- **Keep the static as a fallback** (the old recommendation). Easiest call site, but none of the problems go away,
  and removing it after release is breaking.
- **Design `IQueryShaper` now.** The configuration it would carry isn't settled yet.
- AutoMapper, the model the call site was based on, removed its static `Mapper` in v9 for the same reasons; its
  `IQueryable` projection (`ProjectTo`) has always taken the configuration explicitly.
**Consequence:** call sites that relied on the fallback pass the instance. `TheStaticFallback_FollowsTheLastServiceCollection`
becomes a check that the configuration is taken from what's passed. Unblocks the configurable And/Or evaluation
order, whose global setting goes on `IQueryShaper`.

### 039: One repository for the preview, one repository per product for production (2026-11-26)
**Status:** accepted (2026-11-26). Settles D6.
**Context:** both libraries share one repository and one solution (`Steverzag.Lab.sln`) by history, not by choice:
the work started as QueryShaper and PropertySelectors turned out to be a product of its own. `QueryShaper`
references PropertySelectors as a project, and the EF add-on references QueryShaper the same way. Most of the
remaining Phase 4 work touches both libraries.
**Decision:**
1. **`0.1.0-preview` ships from this repository.** Cross-library changes keep landing in one commit, tested together.
2. **When going to production (1.0), split into two repositories**, one per product: PropertySelectors in one;
   QueryShaper and its EF add-on in the other. QueryShaper then takes PropertySelectors as a package reference,
   so a change that needs both is a PropertySelectors release followed by a QueryShaper update. Each repository
   keeps the history of its own files, and `decisions.md` and the product docs are split along the same line
   (cross-cutting decisions are copied into both).
3. **For the preview, QueryShaper's package declares its PropertySelectors dependency explicitly** (a version range on
   the released minor, per 032), so packing never produces a dependency on an unreleased in-repo version.
**Considered:**
- **One repository for good** (as dotnet/efcore or `Microsoft.Extensions.*` in dotnet/runtime). Fewer moving parts,
  but the products have separate audiences (README, issues, NuGet project URL).
- **Split before the preview.** Every remaining cross-library change would become two releases while the APIs are
  still moving.
**Consequence:** Phase 5 gets the explicit dependency range; Phase 6 carries the split. The lab (`lab/`) is not a
product and goes to neither by default; where it lives after the split is decided then.

### 040: Base search lookups must be strings (2026-11-26)
**Status:** accepted (2026-11-26). Settles D8.
**Context:** the base search runs `Contains(search)` on each lookup and calls `ToString()` first on a lookup that
isn't a `string` (`FilteringExtensions.CreateContainsExpression`), pinned for numbers by
`BaseSearchTests.NonStringLookup_IsSearchedAsText`. On a database column that's the provider's conversion, not the
text a user sees: a `DateTime` reads differently on SQLite, SQL Server and in memory, a `Guid`'s case varies, and
`bool`, `double` and enums depend on the provider too. A search can then match on one provider and silently match
nothing on another. (The filter conditions aren't affected: they take the text path only for `string` properties,
and reject `Contains` on dates.)
**Decision:**
1. **A base search lookup must declare `string` (or `string?`).** Any other type, a collection of strings included,
   is rejected with a clear message where the lookups are given: when the search is configured
   (`BaseSearchConfiguration.Configure<T>` / `ConfigureSearch`), and when `ApplyBaseFilter` receives lookups
   directly. A selector that produces a string (`Description|$FN_COALESCE("")`) is a string lookup.
2. The `ToString()` fallback is removed.
3. Integers are rejected too, although their text is stable: matching "20" against 200 isn't what a search box
   should do. Other types can be allowed later after a provider check; that's additive.
**Considered:**
- **Keep it and document the provider dependence.** The silent mismatches stay.
- **Strings and integers.** Stable text, but substring matching on numbers surprises more than it helps.
- **Date-aware search** (search text that reads as a date matches a range on date lookups). Useful, but needs
  format and culture rules; kept as a Phase 6 enhancement.
**Consequence:** `NonStringLookup_IsSearchedAsText` becomes a rejection test. Anyone who wants a non-string
property in the search writes a lookup that produces a string.

### 041: The `QueryRequest` contract and the `IQueryShaper` entry point (2026-11-27)
**Status:** accepted (2026-11-27). Builds on 038.2; settles the JSON conversion that 008.3 deferred.
**Context:** a client can't send a query today. Nothing binds from JSON: `PropertySelector` only has implicit
operators, `FilterValue` has private fields and constructors, `FilterNode` is abstract, and the enums bind as numbers.
Calling code chains `ApplyBaseFilter`, `ApplyLogicsFilter`, `ApplyOrder`, `ApplySelect` and `Paginate` itself and
passes the mapper and the search configuration by hand (038.1). `PaginationParameters` (default 10, max 50, clamping)
is internal and unused. A JSON value carries no .NET type: `"2026-11-01"` is a string, but `FilterValue`'s `DateTime`
cast only accepts the `DateTime` kind, so today a date can't be filtered from JSON at all.
**Proposal:**
1. **One non-generic request type**, the same for every entity, bindable with `[FromBody]`:
   ```csharp
   public sealed record QueryRequest
   {
       public string? Search { get; init; }
       public IReadOnlyList<FilterNode> Filters { get; init; } = [];
       public IReadOnlyList<OrderParameters> Order { get; init; } = [];
       public PageRequest? Page { get; init; }
   }
   public sealed record PageRequest(int Index, int Size);
   ```
   ```json
   {
     "search": "phone",
     "filters": [
       { "field": "Price", "condition": "GreaterThan", "value": 10 },
       { "connector": "Or", "filters": [
           { "field": "Created", "condition": "GreaterThanOrEqual", "value": "2026-11-01" },
           { "field": "Tags", "condition": "ContainsAny", "value": ["new", "sale"] } ] }
     ],
     "order": [ { "field": "Created", "direction": "Descending" } ],
     "page": { "index": 2, "size": 20 }
   }
   ```
   - `search` uses only the **configured** base search for the type (`IBaseSearchConfiguration`). The client can't
     send lookups, because choosing which columns are searched is the server's job (see the Phase 6 note on
     searching every string property). `FilterParameters.SearchLookups` stays for use from code.
   - Grouping and aggregation (`ApplyGrouping`/`ApplySelect` → `SelectResult`) are **out**. They change the result
     type, and their request shape is a separate design. They can be added later as `QueryRequest.Aggregate`
     without breaking anything.
2. **The JSON names are the C# names, and the C# names get shorter.** This is pre-release, and Phase 4 is where
   the surface is shaped. `ConditionNode.FieldSelector` → `Field`, `ConditionNode.FilterCondition` → `Condition`,
   `FilterNode.FilterLogicOption` → `Connector`, and `OrderParameters(OrderBy, OrderDirection)` →
   `(Field, Direction)`. They're serialized in camelCase under ASP.NET's web defaults. `connector` defaults to `And`
   and is ignored on the first node (as in `CombineNodes`). `direction` defaults to `Ascending`.
3. **The converters live in QueryShaper and are attached with `[JsonConverter]`**, so ASP.NET binds the request
   with no setup:
   - `PropertySelector`, on `Field` (a property-level attribute): read from a JSON string through the non-generic
     implicit operator. A `SelectorParseException` is rethrown as `JsonException` with the selector's message, so
     the client gets a 400 with a JSON path rather than a 500. PropertySelectors itself stays untouched (001). A
     converter there can be added later.
   - `FilterNode`: the two kinds are told apart by shape, as planned. If `filters` is present it's a `GroupNode`,
     otherwise a `ConditionNode`. A node with both `filters` and `field` is rejected. No `"type"` discriminator is
     needed. `GroupNode`'s existing guards (an empty group, null nodes) surface as `JsonException`.
   - `FilterValue`: read from the JSON token alone.
     - A string becomes `String`.
     - A number that fits `long` and has no fraction becomes `Integer`. Any other number becomes `Decimal`, or
       `Double` if it's outside `decimal`'s range.
     - `true`/`false` becomes `Bool`.
     - An array of those becomes `Enumerable`.
     - `null` becomes a null `Value`.
     - An object or a nested array is rejected.
   - The request's own enums (`FilterCondition`, `LogicalConnector`, `OrderDirection`) are read by name, through
     `JsonStringEnumConverter<T>` on the enum. Numbers are rejected (`allowIntegerValues: false`). This doesn't
     touch filter **values**: an enum property still accepts its underlying number (008.4). Why names only:
     - The members have implicit values, so a number follows their order. Inserting a member (say `Between` after
       `LessThanOrEqual`) would shift every later one, and `"condition": 6` would silently change meaning. With
       names, a new member is additive, as 032 requires.
     - System.Text.Json binds an undefined number (`"condition": 99`) without complaint, and it would fail later,
       deep in the builder. That's the silent reinterpretation 009 removed.
     - Names are readable in logs and bug reports.
     - Accepting numbers later (explicit, frozen member values, with undefined numbers rejected) is additive;
       removing them after release would be breaking.
4. **A string is converted to the property's type when the filter is built** (target typing, 008.3). The converter
   can't see the property, so the filter builder converts a `String` value when the property is `DateTime`,
   `DateOnly`, `TimeOnly`, `TimeSpan`, `DateTimeOffset` or `char`. That's how `Guid` (029) and enum names (008.4)
   already work, and it applies to single values and list elements alike.
   - Parsing is strict and culture-invariant, ISO 8601 only: `yyyy-MM-dd`,
     `yyyy-MM-ddTHH:mm:ss[.fffffff][Z|±hh:mm]`, `HH:mm[:ss[.fffffff]]`, and the constant `c` format for `TimeSpan`.
     A `char` comes from a one-character string.
   - A string that doesn't parse throws `FilterConditionAndValueNotSupportedException` naming the field.
   - This is additive: values that are already typed in code behave as before. What a `Z` or an offset means for a
     `DateTime` property is settled, with tests, when this is built.
5. **`IQueryShaper`, registered by `AddQueryShaper`**, carries the mapper, the search configuration and the options.
   It's granular: each part of a request can be applied on its own, and `Apply` is just those parts in order.
   ```csharp
   public interface IQueryShaper
   {
       IQueryable<T> ApplySearch<T>(IQueryable<T> source, string? search);
       IQueryable<T> ApplyFilters<T>(IQueryable<T> source, IReadOnlyList<FilterNode> filters);
       IQueryable<T> ApplyOrder<T>(IQueryable<T> source, IReadOnlyList<OrderParameters> order);
       IQueryable<TOut> Map<TIn, TOut>(IQueryable<TIn> source);
       PageRequest ResolvePage(PageRequest? page);                                       // defaults and limits

       IQueryable<T> Apply<T>(IQueryable<T> source, QueryRequest request);               // search → filters → order
       IQueryable<TOut> Apply<TIn, TOut>(IQueryable<TIn> source, QueryRequest request);  // … then map
   }
   // core: Paginate(this IQueryable<T>, PageRequest), plus ApplyPaged<T> / <TIn, TOut> as extensions on IQueryShaper
   // EF:   PaginateAsync(this IQueryable<T>, PageRequest, ct), plus ApplyPagedAsync<T> / <TIn, TOut>
   ```
   - The order of operations is base search → filters → order (all on `TIn`) → map → page. Selectors name the
     **source** type's properties, as they do today, and that's what EF translates. Letting clients use the DTO's
     names is a later feature: an allow-list with aliases belongs with the guard rails.
   - An empty request is valid: no search, no filters, no order, and the default page. An empty part is a no-op,
     so a request with only filters only filters.
   - Each granular method validates its input and applies the options exactly as `Apply` does for that part, so
     every route through `IQueryShaper` gets the same checks and, later, the same guard rails.
   - The interface's methods share names with the extensions (`ApplyFilters`, `ApplyOrder`). That's acceptable:
     one is an instance method, the other an extension in another namespace (`Configuration` vs. `Filtering`).
6. **Two public layers: the extensions are the trusted layer, `IQueryShaper` the guarded one.**
   - The extension methods (`ApplyBaseFilter`, `ApplyLogicsFilter`, `ApplyOrder`, `ApplySelect`, `Paginate`)
     stay public. They turn values built in code into expressions, with no dependency injection, no options and
     no guard rails. That keeps today's lightweight use, and the lab and the tests keep using them directly.
   - `IQueryShaper` is a thin layer over them: it validates, reports paths, applies the options and the guard
     rails, then calls the extensions. No filtering, ordering or paging logic lives in it, so a change lands in one
     place. Both layers fall under 032's contract.
   - The README and the extensions' XML docs say it plainly: the extensions are for trusted input built in code;
     client requests go through `IQueryShaper`. Passing client input straight to an extension bypasses the
     guard rails.
   - Later, additive: extension overloads that take the guard-rail options explicitly
     (`query.ApplyFilters(filters, options)`), for callers who want the protection without dependency injection.
7. **`IQueryShaper` validates its whole input before building anything, and reports where each problem is.**
   - Each selector is first parsed again against the source type (`PropertySelector<TIn>`), so the generic form's
     parse-time typing (018) catches unknown properties and type mismatches, with their positions.
   - Every failure, in a selector or a filter value, is thrown as a new `QueryRequestException`. It carries a `Path`
     in the request (`filters[1].filters[0].field`, `order[0].field`, `page.size`) and the original exception as its
     `InnerException`. A server maps that one type to a 400.
   - The path is relative to what the method received: `filters[1].field` from `Apply`, `[1].field` from
     `ApplyFilters`.
   - A non-blank `search` on a type with no search configured stays `InvalidOperationException` (010.8). That's a
     server bug, not a bad request.
8. **Options go through `Microsoft.Extensions.Options`.** `AddQueryShaper<TConfiguration>(Action<QueryShaperOptions>?
   configure = null)` calls `services.Configure`, so several calls compose in the standard way.
   - For this decision the options hold only the paging settings: `DefaultPageSize` (10) and `MaxPageSize` (100).
   - A requested size above the maximum is **rejected** (`QueryRequestException` at `page.size`), not clamped, in
     line with 009.
   - A request with no `page` gets index 1 and the default size, so a request can never return every row.
   - The guard rails (the allow-list, depth, the filter count) and the evaluation order (the 007 follow-up) become
     properties on the same options class later.
9. **`PaginationParameters` is deleted.** `PageRequest` and the options replace it.
**Considered:**
- **A generic `QueryRequest<T>`** whose converters parse typed selectors at bind time. Errors would come with a
  JSON path for free, but `FilterNode` would need a generic twin and every endpoint a type argument. Point 7 gets
  the same typing at `Apply` time.
- **Only a global `Apply` on `IQueryShaper`.** A caller who wants only the filters would have to use the raw
  extensions and lose the validation and, later, the guard rails, which would be a security gap.
- **Making the extensions internal**, so `IQueryShaper` is the only way in. Every user would need dependency
  injection just to filter a query, and the lab and the tests would have to go through the container.
- **Verb-only names on the interface** (`Filter`, `Order`, `Search`) to tell it apart from the extensions. Possible,
  but different names for the same operation are harder to learn than the same name on two layers.
- **Enum numbers on the wire.** See point 3: they follow member order and let undefined values through.
- **A `"type"` discriminator with `[JsonPolymorphic]`.** It's built in, but clients have to write
  `"type": "condition"` on every node, and it has to come first in the object.
- **`FilterValue` holding a `JsonElement`**, converted when the filter is built. It keeps the JSON exactly, but it
  ties the filter builder to System.Text.Json, and values built in code and from JSON would follow two paths.
- **Clamping an oversized page** (GitHub's model). Friendlier, but a client that asks for 500 and silently gets 100
  can't tell it has missed rows.
- **GET, with the request in the query string.** Not for the preview: the filter tree doesn't fit query-string
  binding. A single `?q=` JSON parameter can be added later on top of the same converters.
**Consequence:**
- Point 2 is a breaking rename. It touches every test and lab call site that builds a `ConditionNode` or
  `OrderParameters`.
- New dependency: `Microsoft.Extensions.Options`, a framework package like the DI abstractions already referenced.
- The EF add-on gains `PaginateAsync(PageRequest)` and `ApplyPagedAsync`.
- Tests:
  - converter round-trips and rejections for each shape and each value kind;
  - string-to-type conversion for each date/time type, with valid and invalid input;
  - `Apply` on SQLite producing the same SQL as the hand-chained extensions;
  - `Path` for each place a failure can happen, from `Apply` and from each granular method;
  - each granular method producing the same SQL as its part of `Apply`;
  - paging defaults, the cap and the empty request.
- Docs: the README and the extensions' XML docs state the two-layer rule (point 6). This is the one exception to
  holding XML docs back until the Phase 4 pass, since it's a security note.

**Implemented (2026-11-27).** As written, with these details settled while building it:
- **Where things live.** `IQueryShaper`, `QueryRequest`, `PageRequest` and `QueryShaperOptions` are in the root
  namespace `Steverzag.QueryShaper`, not `Configuration` (point 5's note on names still holds: the extensions are in
  `Filtering`, `Ordering`, …). `QueryRequestException` is in `Exceptions`. The implementation, `DefaultQueryShaper`,
  is internal; the converters are internal in `Serialization`.
- **`PageRequest(int Index = 1, int? Size = null)`**, not `(int Index, int Size)`. A client can send
  `{ "page": { "index": 3 } }` and get the default size, instead of a 0 that would have to mean "default".
  `Paginate(PageRequest)` and `PaginateAsync(PageRequest)` need a size, and say to use `ResolvePage` if it's missing.
- **The JSON path of a binding error.** A custom converter can't see the path above it, and a nested `Deserialize`
  restarts at `$`. So `JsonException.Path` points at the **top-level** filter (`$.filters[1]`), and the message
  names the place inside it (`… (at 'filters[0].condition' in this filter)`). The converter reads the whole node into
  a plain recursive shape in one call, so the inside paths are System.Text.Json's own. Errors in `order` and `page`
  have their exact path.
- **`condition` is required in JSON.** Leaving it out would otherwise default to `Equals` (the enum's 0), the kind
  of silent reinterpretation 009 removed. A null node in a group is "A filter can't be null." with its place.
- **`OrderParameters` has a converter too**, so a missing `field` is a `JsonException`, not an
  `ArgumentNullException` from the constructor (a 500). Its `Field` stays a string; it's parsed when ordered.
- **Text conversion.** A `DateTime` with `Z` or an offset becomes UTC; one without is taken as written (Kind
  `Unspecified`). `DateTimeOffset` requires `Z` or an offset. `Guid` text is now accepted in lists too (it already was
  for single values), since JSON can only send a Guid as a string. `TimeSpan`, `DateTimeOffset` and `char` have no
  single-value filter branch, so their text conversion applies in lists only.
- **Error mapping in `IQueryShaper`.** `SelectorParseException` → `.field`; `FilterNotSupportedException` →
  `.condition`; `FilterConditionAndValueNotSupportedException`, `InvalidCastException`, `FormatException` and
  `ArgumentException` (an empty list) → `.value`. Anything else passes through unwrapped: it's a bug, not a bad
  request. The extensions still throw their own exceptions.
- **Options are validated when `IQueryShaper` is first resolved** (`InvalidOperationException`: a size below 1, or a
  default above the maximum), not at `AddQueryShaper`, because the standard options pattern lets later calls change
  them.
- **Enums.** A small name-only converter instead of `JsonStringEnumConverter`, which can't be told through an
  attribute to reject numbers, and which would also accept comma lists (`"Equals, NotEquals"`).
- Tests: 152 in `tests/Steverzag.QueryShaper.Tests/Requests/`. 58 in `JsonBindingTests`: shapes, rejections, value
  kinds, enums, paths, round trip. 46 in `TextValueConversionTests`, on SQLite plus `DateTimeOffset` in memory.
  48 in `QueryShaperTests`: registration and options, the same SQL as the hand-chained extensions for `Apply` and
  each granular method, every error path, paging, and EF `ApplyPagedAsync`. All 701 existing QueryShaper tests pass
  after the rename.

### 042: A `MemberFilter` hides members from the tree and rejects them in client selectors (2026-11-27)
**Status:** accepted; implemented (2026-11-27). Follows the member-filter item agreed 2026-11-26.
**Context:** `SelectorsMetaDataTree` lists everything a selector path can reach, which is right for in-memory data. On a
database it offers members that don't translate, or translate wrongly, and a client can send any of them whatever the UI
offers. What translates depends on the provider, and PropertySelectors can't know it (006).
**Decision:**
1. **One rule for both jobs.** `MemberFilter` (root namespace) has `All` (the default), `Portable`, `Allows(type,
   property)`, `Except(hide)`, `Only(allow)` and `IsAll`. The type passed is the one the member is read from: the
   underlying type of a nullable, the element of a collection projection.
2. **The tree:** `ExplorePropertiesTree(maxDepth, filter = null)` skips a hidden member and its subtree.
3. **Parsing:** `filter.Parse<T>(selector)` parses in the generic form with the filter active, and
   `TypesExtensions.GetPropertyType`, which resolves every property path the generic form parses, checks each member.
   So the head, a piped segment, function arguments, arithmetic operands and ordering arguments are all covered, and the
   error is a `SelectorParseException` at the member's position: "Property 'Ticks' on 'DateTime' is not allowed."
   The filter is a thread-static scope like `SelectorLimits`' counters. It's active only inside `Parse<T>`, so a plain
   parse is unchanged. The parsed selector is checked, not the built expression, because built-in functions produce
   member accesses of their own that the client never wrote.
4. **QueryShaper:** `QueryShaperOptions.MemberFilter`, default `All`. `IQueryShaper` parses every `field` in `filters`
   and `order` through it (`OrderParameters<T>` included), so a hidden member is a `QueryRequestException` at the
   field's path. The configured search and the mapper are server configuration and aren't filtered. A null filter
   fails when the shaper is resolved. The extensions, the trusted layer (041.6), don't filter.
5. **`Portable` is an allow-list for the framework date and time types only** (`DateTime`, `DateTimeOffset`,
   `DateOnly`, `TimeOnly`, `TimeSpan`). Any member not listed is hidden, so a member a future .NET adds stays hidden
   until it's probed. Other types are untouched. Its contents come from a probe on SQLite, SQL Server 2022 and
   PostgreSQL 17 (`tests/Steverzag.ProviderTests`, Testcontainers). The probe filters `member == value` for every
   distinct seeded value, through a plain and a nullable column, and compares the rows with the same expression in
   memory over the rows as read back. A filter is used rather than a projection, because EF evaluates an
   untranslatable final projection on the client without saying so.
   - In: `DateTime` `Year`, `Month`, `Day`, `Hour`, `Minute`, `Date`, `DayOfYear`; `DateOnly` `Year`, `Month`,
     `Day`, `DayOfYear`.
   - Out, with the reason:
     - `Second` (on `DateTime`, `DateTimeOffset` and `TimeOnly`; `TimeSpan.Seconds`): **wrong rows on PostgreSQL**.
       It rounds the fractional second instead of truncating it, so `23:59:59.999` gives 60.
     - `DayOfWeek`: not translated on SQL Server.
     - `Millisecond`: not translated on PostgreSQL.
     - `DateTime.TimeOfDay`: wrong rows on SQLite (the `TimeSpan` text format, as in 028).
     - `DateTime.Ticks`: wrong rows on SQLite, not translated elsewhere.
     - `Kind`, `Microsecond`, `Nanosecond`: not translated anywhere.
     - Every `DateTimeOffset`, `TimeOnly` and `TimeSpan` member: SQLite translates none of them.
   - The seeds are limited by storage: PostgreSQL's `timestamptz` takes only UTC offsets, and SQL Server stores a
     `TimeSpan` as `time` (under a day). The full matrix is printed by the probe's `Report` tests.
**Considered:**
- **Checking the built expression** with a visitor. It needs no parse hook, but it would blame the client for member
  accesses that built-in functions emit, and its errors have no position.
- **Walking the parsed tree** after parsing. It would duplicate the generic form's type resolution (projection,
  lifting, the argument scope of 031), and the tree doesn't keep positions.
- **A deny-list preset** (`Ticks`, `Kind`, `Nanosecond`). Simpler, but a new or forgotten member would be offered by
  default.
- **`Portable` as QueryShaper's default.** Safer for EF users, but it would break selectors that work on the provider
  in use today. It's opt-in.
**Consequence:** additive. Docker is needed to run the provider tests locally, and CI runs them (GitHub's Ubuntu
runners have it). Still to do: a matching rule for functions in `FunctionCatalog`, and a provider-specific preset from
the EF add-on. Both fit the same class later. Tests: 52 in `MemberFilterTests` (PropertySelectors), 13 in
`MemberFilterRequestTests` (QueryShaper), and 6 in `Steverzag.ProviderTests` (per provider: every allowed member filters
correctly, and the report).

### 043: Typed member rules, and one rules class per entity (2026-11-27)
**Status:** accepted; implemented (2026-11-27). Builds on 042.
**Context:** 042's only way to name a member was a predicate over `(Type, PropertyInfo)`. It's easy to write one that
checks only `property.Name`, which hides that name on every type the shaper handles. And a project with many entities
would end up with every rule in one place.
**Decision:**
1. **Typed rules on `MemberFilter`:** `Hide<T>(params Expression<Func<T, object?>>[])`,
   `Only<T>(params Expression<Func<T, object?>>[])` and `For<T>(Action<MemberRulesBuilder<T>>)`, whose builder has the
   same `Hide` and `Only`. The type is part of the rule, not of the filter. The filter stays global, and a rule on
   `Customer` applies wherever `Customer` is reached, including from another root (`Orders.Customer.PasswordHash`).
2. **Matching:** a rule applies when the type the member is read from is `T` or derives from it, and the property has
   the same name. So it covers derived types (and a `new` member with that name), but not `T`'s base types or siblings.
   `PropertyInfo` equality isn't used, because reflection returns a different object for an inherited property seen
   from a derived type.
3. **Lambdas:** only a direct property of the parameter (`x => x.Name`, with the compiler's `Convert` for value types).
   A nested path (`x => x.Home.Street`) is rejected with a hint to use `For<Address>`, because the engine can only say
   "`Street` on `Address`", which would hide it everywhere, not just under `Home`. Fields, methods, constants, other
   variables and an empty list are rejected too. Every error is an `ArgumentException` when the filter is built,
   which is at startup.
4. **Composition:** each call returns a new filter that must also pass the new rule, so the strictest result wins.
   `Hide`s add up, two `Only` lists on one type intersect, and `Only` plus `Hide` removes the hidden member from the
   allowed ones. A builder kept after `For` returns can't change the filter, because the rules are copied when it's built.
5. **One class per entity:** `MemberRules<T>` with `Configure(MemberRulesBuilder<T>)`, like EF's
   `IEntityTypeConfiguration<T>`. `Apply(params MemberRules[])` adds instances with no reflection.
   `ApplyFromAssembly(assembly)` adds every non-abstract, non-generic `MemberRules<T>` with a public parameterless
   constructor, ordered by full name (the result doesn't depend on the order). The non-generic `MemberRules` base can't
   be subclassed outside the library.
6. The predicate `Except`/`Only` stay, for rules no lambda can name, such as an attribute of the caller's own
   (`p.IsDefined(typeof(SensitiveAttribute))`). The library ships no attribute: entities often live in a project that
   shouldn't reference a query library.
**Considered:**
- **A filter bound to a root type** (`MemberFilter.PortableOf<Customer>()`). It reads well, but the filter is global,
  so it would suggest the rules apply only to `Customer` queries. Per-root filters are a separate feature: an
  overload on `IQueryShaper` or named options, if a project needs different rules per endpoint.
- **Lambdas read as paths** (hide `Street` only under `Customer.Home`). It would need the check to know the path from
  the root, which pipes and argument scopes (031) make fragile.
- **Throwing on a second `Only` for the same type.** Stricter, but it makes splitting rules across modules awkward.
  Intersecting fails safe.
- **Trimming annotations on `ApplyFromAssembly`.** The library has none yet, `SelectorRegistry.RegisterAssembly`
  included. They belong to one pass over the whole library. `Apply` is the trim-safe route.
**Consequence:** additive. Tests: 16 in `MemberRulesTests` (PropertySelectors). `MemberFilterRequestTests` now uses
the typed form.

### 044: One date/time member deep, `MemberContext`, and provider presets (2026-11-27)
**Status:** accepted; implemented (2026-11-27). Corrects 042.5 (what `Portable` promises) and builds on 043.
**Context:**
- **A probe of two-step chains found that 042's `Portable` was wrong.** On SQL Server `CreatedAt.Date.Hour` and
  `.Date.Minute` fail: EF turns `Date` into a SQL `date`, and SQL Server rejects `DATEPART(hour, date)`. `Portable`
  allowed both, because a filter decided per (type, member), and `Hour` on `DateTime` is fine on a real column.
  Translation depends on the database type of the value, and that depends on the member before it. 042's tests
  missed it: they probed members on columns only, and the explorer stops at a type it has already visited, so
  `At.Date` is a leaf.
- **The same probe, for wider presets, found more:**
  - SQL Server: `DateTimeOffset.Date` followed by `Hour`, `Minute`, `Second`, `Millisecond` or `TimeOfDay` fails the
    same way.
  - PostgreSQL: `TimeOfDay` becomes `time`, and `.Days` on it errors ("unit "day" not supported"), while
    `.TotalDays` gives wrong rows.
  - Every broken chain is useless in practice: `Date.Hour` and `TimeOfDay.Days` are always 0, and
    `TimeOfDay.Hours` is just `Hour`.
- **SQLite only:** the first probe found SQLite translates no member of `DateTimeOffset`, `TimeOnly` or `TimeSpan`, so
  `Portable` allows none of them. Projects that use SQLite only in tests asked for wider presets.
- **The docs claimed something untested:** they said such a value "can still be selected and compared", but whole
  values had never been probed.
**Decision:**
1. **`MemberContext(Type, Property, Source, SourceType)` is public.** `Allows(MemberContext)`,
   `Except(Func<MemberContext, bool>)` and `Only(Func<MemberContext, bool>)` sit next to the `(Type, PropertyInfo)`
   overloads; a lambda's parameter count picks the overload.
   - `Source` is the property the value was read from, `SourceType` the type it was read on. Both are null on the root,
     after a function or after a literal.
   - Inside a path, the source is the previous segment. Across `|` and out of a parenthesized group, it's the last
     property of the path before. The generic `Chain` records that before parsing a path part, and
     `GetPropertyType` reads it.
   - A path remembers its last property in a `ConditionalWeakTable`, only while a filter is active. So record
     equality and plain parses are untouched.
   - In the tree, a node's source is its parent.
2. **`Portable` is one date/time member deep.** A member of one of the five types can't be read from a value that
   another member of those types produced. The error says why: "… is not allowed after 'Date'." `CreatedAt.Date`
   stays, which keeps the most useful member. Rejected alternatives: hiding `Date` and `TimeOfDay` instead, or
   probing and allow-listing chain by chain.
3. **Provider presets** in the EF add-on: `ProviderMemberFilters.Sqlite`, `.SqlServer` and `.PostgreSql`, plus
   `For(DbContext)` and `For(string providerName)`. An unknown provider throws `NotSupportedException` and points to
   `Portable`.
   - They're built from the public typed API (`Only<T>` and a context `Except`), so they show it works for a
     package outside PropertySelectors.
   - Each preset allows a member if it gives the right rows on that provider through a plain and a nullable column,
     and each is one member deep. The contents are in query-shaper.md.
   - Out on the provider where they fail: `Second` on PostgreSQL (it rounds), `TimeOfDay` on SQLite (wrong rows),
     `Ticks`, and `DayOfWeek` on SQL Server.
4. **Whole values aren't members, so no filter restricts them.** They're probed and documented. On SQLite,
   `DateTimeOffset` and `TimeSpan` support only `==`: `>` and `<` aren't translated, and ordering throws
   `NotSupportedException`. Everything else (`==`, `>`, `<`, ordering on all five types) works on all three providers.
5. **What the provider tests pin, per provider:**
   - every path in the filtered tree, for `Portable` and for the preset, filters correctly;
   - every two-step chain the filter accepts, dotted and piped, filters correctly;
   - the preset allows exactly the members that pass on columns, so an EF update that changes what translates fails
     in either direction;
   - `Portable` is exactly what all three presets allow;
   - whole values behave as documented.
**Considered:**
- **Per-pair rules** (hide `Hour` only after `Date` on SQL Server). They'd be precise, but every new pair needs
  probing, and the pairs it would allow are all useless.
- **Keeping the chain rule internal**, with `InternalsVisibleTo` for the EF package. Cross-package internals couple
  the versions, and the same kind of rule is useful to callers.
**Consequence:**
- **Breaking against 042, before any release:** `Portable` now rejects `CreatedAt.Date.Year` and similar chains.
- Members read from a function result are checked by type only, and no probe covers them.
- Tests:
  - 21 more in `MemberFilterTests`: chains through `.`, `|`, groups, arguments and arithmetic; what a context
    predicate sees; context rules; no leaking across functions or parses; the tree.
  - The provider tests: 27 in all, 8 per provider plus 3 that need no database.

### 045: An order on a type the database can't sort is a request error (2026-11-27)
**Status:** accepted; implemented (2026-11-27). A guard rail for the item in Phase 4. Builds on 044.4.
**Context:** member filters only cover members. On SQLite, `"order": [{ "field": "StartsAt" }]` on a `DateTimeOffset`
passed every check, then EF threw `NotSupportedException` when the query ran. A server reports that as a 500, not a
400. A probe ordering 19 types on each provider found four that SQLite can't `ORDER BY`: `decimal`, `ulong`,
`DateTimeOffset` and `TimeSpan`. `UseDecimalMathAsDouble` doesn't change that. SQL Server and PostgreSQL sort every
probed type.
**Decision:**
1. **`QueryShaperOptions.UnsortableTypes`** (`ISet<Type>`, empty by default, so nothing changes until it's filled).
   `IQueryShaper` checks the **built** type of every `order` field, with a nullable unwrapped. Functions are covered
   too, so `Price|$FN_MATH_ROUND` and `$FN_MATH_MAX(Price, 1)` are caught as `decimal`. A match is a
   `QueryRequestException` at `order[i].field`: "'Price' can't be ordered: the database can't sort decimal values."
   - Filters aren't affected.
   - The trusted extensions don't check, as for the other guard rails (041.6).
   - A null set fails when the shaper is resolved.
2. **`ProviderUnsortableTypes`** in the EF add-on: `Sqlite` (the four types), `SqlServer` and `PostgreSql` (empty),
   plus `For(context)` and `For(providerName)`. They're read-only sets that callers add to the option
   (`o.UnsortableTypes.UnionWith(…)`), so their own value types can join them.
3. **Sorting in a different order isn't blocked.** SQL Server's `uniqueidentifier` order and PostgreSQL's
   nulls-last are consistent, just not .NET's, so they're documented, not rejected.
4. **The provider tests pin it:** each set is exactly the probed types the provider can't order, and with the set,
   every probed column ordered through `IQueryShaper` is either rejected up front or runs.
**Considered:**
- **A per-provider preset object holding both the member filter and the unsortable types**
  (`o.UseProvider(name)`). One line, but it would replace a `MemberFilter` the caller had already set. Two explicit
  lines compose better. It can be added later.
- **Rejecting non-scalar orders** (a navigation, a collection) in the same check. It's a different failure, and a
  type with a value converter can be sortable, so it would need its own design.
**Consequence:** additive. Tests: 10 in `OrderGuardTests` (QueryShaper, SQLite), and 6 in the provider tests (2 per
provider). The provider tests now have a `Scalar` entity with one column per common scalar type.
