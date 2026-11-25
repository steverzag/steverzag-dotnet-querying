# PropertySelectors

**Purpose:** describe *"which value of a `T`"* as a string, and get back a LINQ `Expression` that a
query provider (EF Core, LINQ-to-objects) can translate.

**Use it when** a property, computed value or aggregate is chosen at runtime: sort columns sent
by a UI, saved report definitions, dynamic filters, or mapping configuration. If the selection is
known at compile time, a plain lambda is the better tool.

**Non-goals:**
- Executing queries, filtering, or pagination. Those belong to QueryShaper.
- Being a general-purpose expression language. The grammar stays small and SQL-translatable.

## Quick look

```csharp
PropertySelector<Person> name   = "Name";                               // p => p.Name
PropertySelector<Person> city   = "Address.City";                       // p => p.Address.City
PropertySelector<Person> avgId  = "Phones.Id|$FN_AGGR_AVG";             // p => p.Phones.Select(x => x.Id).Average()
PropertySelector<Person> calc   = "(Salary + 100) * 2";                 // arithmetic
PropertySelector<Person> pow    = SelectorDefaults.MathPower + "(2, 2)";// function with arguments
PropertySelector<Person> round  = "Salary|$FN_MATH_ROUND(2)";           // piped: Math.Round(p.Salary, 2)
PropertySelector<Person> lit    = "42";                                 // literal value
PropertySelector<Person> since  = "$FN_DT_DATEONLY(\"2024-01-01\")";    // date literal

Expression body = avgId.PropertyAccess(parameter);
```

## Grammar (informal, not frozen yet; see roadmap Phase 4)

| Construct | Form | Example |
|---|---|---|
| Property path | `Name(.Name)*` | `Address.City` |
| Pipe / chain | `part\|part\|…`, where each part applies to the previous result | `Phones.Id\|$FN_AGGR_SUM` |
| Function | `$FN_<NAME>` or `$FN_<NAME>(arg, …)`. Arguments are selectors themselves, resolved against the scope root. A function gets its subject from the pipe or from its first argument; see [Piped and static forms](#piped-and-static-forms). | `$FN_STR_SUBSTRING(0, 3)`, `Price\|$FN_MATH_ROUND(2)` |
| Number literal | Read with the invariant culture on every machine (decision 017). An integer is `int`, or `long` if it doesn't fit. A `.` or an exponent makes it `double`. The suffix `L` gives `long` and `m` gives `decimal`. A minus sign in front of a number is part of it. A number that doesn't fit its type is rejected. | `55`, `-3`, `1.5`, `1e-3`, `5L`, `9.99m` |
| Boolean literal | `true` / `false`, in any case. | `true` |
| String literal | `"quoted string"`. Everything between the quotes is text, including `\| + - * / ( ) ,`. Write a `"` inside as `""`. | `"Hi"`, `"n/a"`, `"say ""hi"""` |
| Date/time literal | A constructor function taking one quoted string, read with the invariant culture and checked when the selector is parsed. A date-time keeps its kind (a trailing `Z` stays UTC). | `$FN_DT_DATETIME("2025-01-31T08:30:00")`, `$FN_DT_DATEONLY("2025-01-31")`, `$FN_DT_TIMEONLY("08:30")` |
| Arithmetic | `+ - * /`, unary `-`, parentheses. Operands are paths, literals or chains. Both operands are widened to their common type, which is the result type (`Quantity + 1` → `int`, `Price * 3` → `decimal`, nullable if either operand is). | `Amount - 3 - 2` |

`SelectorLiteral.Format(value)` writes any of these literals from a .NET value (`int`, `long`, `double`, `float`,
`decimal`, `bool`, `string`, `DateTime`, `DateOnly`, `TimeOnly`). Use it instead of building literal text by hand.
Values typed by end users should travel as filter values (QueryShaper's `SearchValue`), not inside selector text.

All tokens are constants on `SelectorDefaults`. Prefer those constants to string literals in your code.

## Function catalog

| Family | Tokens |
|---|---|
| Aggregation | `AGGR_COUNT`, `AGGR_LONG_COUNT`, `AGGR_SUM`, `AGGR_AVG`, `AGGR_MIN`, `AGGR_MAX` (`AGGR_COUNTDISTINCT` is declared but not implemented) |
| Element | `ELM_FIRST`, `ELM_LAST`, `ELM_ATINDEX` |
| Ordering | `ORDER_ASC`, `ORDER_DESC`, `THEN_ORDER_ASC`, `THEN_ORDER_DESC` |
| Math | `MATH_CEILING`, `FLOOR`, `ROUND`, `TRUNCATE`, `SIGN`, `POW`, `SQRT`, `EXP`, `LOG`, `LOG10`, `SIN`, `COS`, `TAN`, `ASIN`, `ACOS`, `ATAN`, `ATAN2`, `MAX`, `MIN` |
| String | `STR_LENGTH`, `TOUPPER`, `TOLOWER`, `TRIM`, `TRIMSTART`, `TRIMEND`, `SUBSTRING`, `INDEXOF`, `REPLACE`, `CONTAINS`, `STARTSWITH`, `ENDSWITH`, `CONCAT`, `JOIN`, `COMPARE`, `ISNULLOREMPTY`, `ISNULLORWHITESPACE` |
| Date | `DT_DATETIME`, `DATEONLY`, `TIMEONLY` (literals), `DT_ADDYEARS`, `ADDMONTHS`, `ADDDAYS`, `ADDHOURS`, `ADDMINUTES`, `ADDSECONDS`, `ADDMILLISECONDS`, `NOW`, `UTCNOW`, `TODAY`. The family is meant to grow (decision 017.9; roadmap). |
| Null handling | `COALESCE` |

**Aggregate result types** follow `Enumerable`'s overload for the element type, and the selector's `Type` matches
the expression it builds. `AGGR_MIN`, `AGGR_MAX` and `AGGR_AVG` first lift a non-nullable value element to its
nullable form (`Numbers.Select(x => (int?)x).Max()`), so **an empty collection gives `null`**, both in memory and
in SQL (decision 014):

| Function | Element type → result |
|---|---|
| `AGGR_SUM` | the element type: `int` → `int`, `decimal?` → `decimal?`, … An empty collection sums to 0. |
| `AGGR_MIN`, `AGGR_MAX` | the nullable element type: `int` → `int?`, `decimal?` → `decimal?`, … |
| `AGGR_AVG` | `int`, `long`, `double` → `double?`; `float` → `float?`; `decimal` → `decimal?` (nullable elements give the same) |
| `AGGR_MIN`, `AGGR_MAX` on other comparable types | `DateTime` → `DateTime?` (any value type is lifted); reference types such as `string` stay as they are, through the generic overload |

`AGGR_SUM` and `AGGR_AVG` support the numeric types `int`, `long`, `float`, `double` and `decimal`, and their nullable
forms. Anything else throws `InvalidOperationException`: `AGGR_AVG` when the selector is parsed, and `AGGR_SUM` when the
expression is built.

**Math result types.** `ROUND`, `CEILING`, `FLOOR`, `TRUNCATE`, `MAX` and `MIN` use the `Math` overload for the operand
type when there is one (`MAX` on two `int`s is `Math.Max(int, int)` → `int`; on `int` and `long` it is `long`), and
`double` otherwise (`CEILING` on an `int` → `double`). **Decimals go through `double`**: EF's SQLite provider can't
translate `Math` over `decimal`, and the core can't see the provider (decision 006). For money this can lose precision
(`Math.Round(0.5015m, 3)` is 0.502, but through `double` it is 0.501). The roadmap tracks an opt-in fix. The other math
functions return `double`, and `SIGN` returns `int`. Note that SQL and .NET round halves differently: SQLite rounds
4.5 to 5, and .NET's `Math.Round` gives 4 (to even).

**Declared type (`Type`).** A selector's declared type is either the type of the expression it builds, or `null` when
that can't be known until the expression is built. It is never a different type. In the generic form it is known
when the selector is parsed, function arguments included (decision 018): `$FN_MATH_MAX(Amount, 5.0)` declares `double`
and `Quantity|$FN_MATH_MAX(Views)` declares `long` on a `long` `Views`. In the non-generic form a property has no type
until the expression is built, so anything that depends on one declares `null`. `ToGeneric<T>()` reparses the text
and gives you the types.

**Typed function arguments (decision 018).** In the generic form, each argument is parsed in the generic form against
its scope type:
- **The scope root** for almost every function. In `PropertySelector<Order>`, the `Views` in `Quantity|$FN_MATH_MAX(Views)` is read on
  `Order`, the same place it is resolved when the expression is built.
- **The element of the target** for the ordering functions. In `Lines|$FN_ORDER_ASC(Weight)`, `Weight` is read on a
  line. Functions nested inside that argument resolve against the line too.

So an unknown property inside an argument fails **when the selector is parsed** in the generic form ("Property
'Missing' was not found on type 'Order'"). In the non-generic form it still fails when the expression is built.
Structural errors are reported first: a wrong argument count or a rejected pipe gives its own message even if an
argument also names an unknown property.

## Piped and static forms

A function gets the value it works on, its *subject*, in one of two ways (decision 016):

- **Piped:** the subject is the value before the `|`. `Name|$FN_STR_TRIM`, `Price|$FN_MATH_ROUND(2)`.
- **Static:** the subject is the first argument, and the function starts the chain. `$FN_MATH_ROUND(Price, 2)`.

**The rule: the piped value is the first argument.** `Price|$FN_MATH_ROUND(2)` and `$FN_MATH_ROUND(Price, 2)` build
the same expression. To turn one into the other, move the first argument in front of the `|`. Every other argument
resolves against the scope root in both forms, so `Details.Price|$FN_MATH_ROUND(Digits)` reads `p.Digits`, not
`p.Details.Digits`.

| Functions | Piped | Static |
|---|---|---|
| `AGGR_*`, `ELM_*`, `ORDER_*`, `THEN_ORDER_*`, the instance string functions (`STR_LENGTH`, `TOUPPER`, `TOLOWER`, `TRIM*`, `SUBSTRING`, `INDEXOF`, `REPLACE`, `CONTAINS`, `STARTSWITH`, `ENDSWITH`), `COALESCE` | ✅ the usual form | ❌ (at the head of a chain the subject is the root itself, e.g. `$FN_AGGR_COUNT` on a group) |
| every `MATH_*`, `STR_CONCAT`, `STR_COMPARE`, `STR_ISNULLOREMPTY`, `STR_ISNULLORWHITESPACE`, every `DT_ADD*` | ✅ | ✅ |
| `DT_NOW`, `DT_UTCNOW`, `DT_TODAY`, `DT_DATETIME`, `DT_DATEONLY`, `DT_TIMEONLY`, `STR_JOIN` | ❌ rejected | ✅ the only form |

```csharp
"Created|$FN_DT_ADDDAYS(3)"                     // p.Created.AddDays(3); declares DateOnly on a DateOnly
"Name|$FN_STR_TRIM|$FN_STR_CONCAT(\"!\")"        // string.Concat(p.Name.Trim(), "!")
"Reviews.Stars|$FN_AGGR_MAX|$FN_COALESCE(0)|$FN_MATH_MAX(3)"
"Price|$FN_MATH_ROUND(2) * 3"                   // inside arithmetic
```

- **Symmetric functions** (`MATH_MAX`, `MATH_POW`, `STR_CONCAT`, …) follow the same rule: `A|$FN_MATH_MAX(B)` is
  `Math.Max(A, B)`.
- **Don't repeat the subject.** `Inner|$FN_MATH_MAX(A, 1)` is `Math.Max(Inner, A, 1)`, which is one argument too many.
  It's rejected with `ArgumentException`: "…can't be called with 2 arguments besides the piped value… The piped value
  is its first argument, so 'Inner|$FN_MATH_MAX(…)' means '$FN_MATH_MAX(Inner, …)'."
- **Functions without a subject reject the pipe** when the selector is parsed: "$FN_DT_NOW takes no piped value, but
  'Created' is piped into it. Write it at the start of a chain: '$FN_DT_NOW'." `STR_JOIN` is rejected too, because its
  first argument is the separator, so `Tags|$FN_STR_JOIN(", ")` would mean `string.Join(Tags, ", ")`. Pass the value
  after the separator instead: `$FN_STR_JOIN(", ", Name, Nickname)`.
- **Declared type:** in the generic form the subject's type comes from the chain, so a piped function knows it when
  the selector is parsed (`Day|$FN_DT_ADDDAYS(3)` declares `DateOnly`). Since decision 018 the static form does too,
  because its arguments are typed: `$FN_DT_ADDDAYS(Day, 3)` declares `DateOnly`, and `$FN_MATH_MAX(A, B)` the wider
  of the two types.
- **Limit:** the root is found from the piped value, so it is lost when the chain doesn't start at a property. In
  `$FN_DT_NOW|$FN_DT_ADDDAYS(Days)`, `Days` is looked up on `DateTime`.
- `ToString()` writes the form that was parsed.

## Coalesce: a default for null

`$FN_COALESCE` takes its first operand from the pipe, and its arguments are the fallbacks in order (decision 015):

```csharp
PropertySelector<Product> stars   = "Reviews.Stars|$FN_AGGR_MAX|$FN_COALESCE(0)"; // p.Reviews.Select(r => (int?)r.Stars).Max() ?? 0  → int
PropertySelector<Product> touched = "DeletedAt|$FN_COALESCE(CreatedAt)";          // p.DeletedAt ?? p.CreatedAt                   → DateTime
PropertySelector<Item>    first   = "First|$FN_COALESCE(Second, Third, 0)";         // x.First ?? x.Second ?? x.Third ?? 0
```

- Every argument is a selector (a literal, a property, a chain, a function), resolved against the scope root.
  EF translates the result to SQL `COALESCE`.
- Every operand except the last must be able to be null (`Nullable<>` or a reference type). Otherwise the selector is
  rejected with `InvalidOperationException`, including a non-nullable piped value (`Quantity|$FN_COALESCE(0)` on an `int`).
- Types unify like C#'s `??`: numbers widen (`decimal? ?? 0` → `decimal`, `int? ?? long` → `long`), and other types
  must match exactly (`DateTime?` with `DateTime`, an enum with the same enum). The result is nullable only if the last
  operand is.
- Collections can't be coalesced, and the function needs a piped value: `$FN_COALESCE(a, 0)` at the head of a chain
  is rejected.
- **Declared type (`Type`) when the selector is parsed:** exact in the generic form, property fallbacks included
  (decision 018), so a function can be chained after it (`DeletedAt|$FN_COALESCE(CreatedAt)|$FN_DT_ADDDAYS(1)`). The
  rules above are also checked then, so `LastUpdated|$FN_COALESCE(Name)` is rejected when parsed. In the non-generic
  form a property fallback has no type until the expression is built, except that a string subject declares
  `string`. Code that builds the expression first, such as the QueryShaper mapper, still checks the result type.
- Coalesce works inside arithmetic (`A|$FN_COALESCE(0) + 1`). A quoted default can hold any character, with `"`
  written as `""` (`Nickname|$FN_COALESCE("n/a")`), and a date default is a date literal
  (`DeletedAt|$FN_COALESCE($FN_DT_DATETIME("2024-01-01"))`).

## Generic vs non-generic

- `PropertySelector<T>` knows its root type, so it can resolve property types, function overloads
  and arithmetic on properties. **Use it whenever possible.**
- Non-generic `PropertySelector` exists for DTOs and serialization, where `T` isn't known yet.
  QueryShaper converts it to the generic form internally (`ToGeneric<T>()`). In the non-generic form,
  property paths resolve against the target when the expression is built, so an unknown property
  fails then and not when the selector is parsed.

## Back to text: `ToString()`

`ToString()` returns selector text that parses back to an equivalent selector, in either form. Both
forms give the same text, and `ToGeneric<T>()` and `OrderParameters<T>` rely on it. The text is
normalized, not echoed:

- Function arguments are separated by `, `, and a function without arguments has no parentheses
  (`Reviews|$FN_AGGR_COUNT`).
- Arithmetic gets only the parentheses its structure needs: `(A * B) + C` becomes `A * B + C`, while
  `A - (B - C)` keeps its parentheses.
- Literals are written the way `SelectorLiteral.Format` writes them: `42`, `-3`, `5L`, `9.99m`,
  `true`, `"say ""hi"""`. A double always has a `.` or an exponent (`2.0`, `1E+20`), so it reads back
  as a double.
- Date literals keep the text they were written with (`$FN_DT_DATEONLY("2025-01-31")`).
- A function keeps the form it was written in: `Price|$FN_MATH_ROUND(2)` stays piped, and
  `$FN_MATH_ROUND(Price, 2)` stays static.

## Extending: custom functions

1. Inherit `FunctionPropertySelector<T>` (and the non-generic `FunctionPropertySelector`).
2. Expose public constructors `()` and `(List<PropertySelector>)`.
3. Decorate both with `[SelectorToken("$FN_ACME_MYFUNC")]`, using a vendor-scoped name.
4. Declare the overloads in one static table and override `Signatures` in both records to return it (decision 019).
5. At startup, call `SelectorRegistry.RegisterAssembly(typeof(MyFunc<>).Assembly)`.

```csharp
internal static readonly IReadOnlyList<FunctionSignature> Table =
[
    // $FN_ACME_CLAMP(value, max)
    new([ArgumentConstraint.Numeric, ArgumentConstraint.Numeric],
        (target, args) => args[0],                          // declared type, from the argument types
        p => target => Clamp(ExpressionUtils.ResolveArgument(target, p[0]),
                             ExpressionUtils.ResolveArgument(target, p[1]))),
    // $FN_ACME_CLAMP(value, min, max)
    new([ArgumentConstraint.Numeric, ArgumentConstraint.Numeric, ArgumentConstraint.Numeric], …),
];
```

A signature has:
- **Parameter constraints:** `Any`, `Numeric`, `Integer`, `String`, `Bool`, `DateLike`, `Collection`, `Of(type)`, and
  the literal-only `StringLiteral` and `IntegerLiteral`. A nullable type matches like its underlying type.
- **`Target`** (default `Any`): what the piped value must be, for functions that work on it (`Collection` for the
  aggregates, `String` for `TRIM`).
- **`Rest`** (optional): the constraint for any number of extra arguments, as in `COALESCE` and `STR_JOIN`.
- **A result rule** `(targetType, argumentTypes) → Type?`. Either type may be `null` when it isn't known yet; return
  `null` then rather than guess.
- **`Bind`**, which receives the arguments and returns the behaviour. It runs when the overload is resolved, so it can
  also validate them, and an exception thrown there is a parse error.

**Overload resolution.** Signatures are matched by argument count, then by constraint. If more than one fits, the
one that is no worse for any argument and better for at least one wins: an exact type beats an implicit conversion
(C#'s implicit numeric conversions, or membership of a kind), which beats `Any`. A tie is an error that names the
candidates. Resolution happens when the selector is parsed if the known types settle it, and otherwise when the
expression is built (the non-generic form with an untyped argument).

**Error messages.** A wrong count says which counts are accepted ("$FN_MATH_ROUND can't be called with 3 arguments:
'$FN_MATH_ROUND(1, 2, 3)'. It takes 1 or 2 arguments."). A wrong type names the argument, what it is, and what was
expected ("$FN_MATH_SQRT: argument 1 ('Name') must be a number, not a String: '$FN_MATH_SQRT(Name)'."), the piped
value ("the piped value 'Name' must be a number") or the target ("$FN_AGGR_SUM must be applied to a collection, not a
String"). Count errors are `ArgumentException`s and type errors are `InvalidOperationException`s.

A function that doesn't override `Signatures` keeps the older model: it overrides `InitializeOverloads` and adds the
overload for the count it was given. Its declared type is whatever it sets itself.

The attribute's `Piped` property says what a piped value means for the function:

| `Piped =` | Meaning |
|---|---|
| `PipedForm.Target` (default) | The piped value is the target the function's overload receives, as for `TRIM` or the aggregates. Functions written before 016 behave this way. |
| `PipedForm.FirstArgument` | The function takes its subject as argument 0. When piped, a placeholder for the piped value is inserted as argument 0, so the same overloads serve both forms. Resolve every argument with `ExpressionUtils.ResolveArgument(target, argument)`, which turns the placeholder into the piped value and resolves the others against the scope root. |
| `PipedForm.Rejected` | The function has no subject, and piping into it is an error. An optional `PipedHint` sentence is added to the message. |

The `Arguments` property says what the arguments are typed against in the generic form (decision 018). It must match
where the function resolves them when the expression is built:

| `Arguments =` | Meaning |
|---|---|
| `ArgumentScope.Root` (default) | The scope root, which is what `ExpressionUtils.ResolveArgument` uses. |
| `ArgumentScope.TargetElement` | The element type of the function's target, for functions that build a lambda over the elements (the ordering functions). If the target isn't a collection, the arguments stay untyped. |

Optionally call `SelectorRegistry.Initialize()` at startup, so the one-time assembly scan doesn't
land on the first request. Duplicate tokens throw.

## Metadata for UIs

`new PropertiesExplorer<T>().ExplorePropertiesTree(maxDepth)` returns a `SelectorsMetaDataTree`,
which a front end can use to offer column/field pickers. It lists properties only, not functions; function metadata is
a roadmap item (Phase 4).

## Internals map

| Area | Location |
|---|---|
| Core records, implicit `string` conversion | `PropertySelector.cs`, `FunctionPropertySelector*.cs` |
| String pre-scan, arithmetic entry | `PropertySelectorUtils.cs`, `ArithmeticSelectors/` (tokenizer → parser) |
| Function dispatch | `Registration/SelectorRegistry.cs`, `SelectorTokenAttribute.cs` |
| Literals | `ValueSelectors/ValueSelectorFactory.cs` |
| Coalesce | `NullSelectors/` |
| Member access building | `ExpressionUtils.cs`, `SelectorImplementations/MethodInfos.cs` |
