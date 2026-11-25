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
PropertySelector<Person> lit    = "42";                                 // literal value
PropertySelector<Person> since  = "$FN_DT_DATEONLY(\"2024-01-01\")";    // date literal

Expression body = avgId.PropertyAccess(parameter);
```

## Grammar (informal, not frozen yet; see roadmap Phase 4)

| Construct | Form | Example |
|---|---|---|
| Property path | `Name(.Name)*` | `Address.City` |
| Pipe / chain | `part\|part\|…`, where each part applies to the previous result | `Phones.Id\|$FN_AGGR_SUM` |
| Function | `$FN_<NAME>` or `$FN_<NAME>(arg, …)`. Arguments are selectors themselves. | `$FN_STR_SUBSTRING(0, 3)` |
| Number literal | Read with the invariant culture on every machine (decision 017). An integer is `int`, or `long` if it doesn't fit. A `.` or an exponent makes it `double`. The suffix `L` gives `long` and `m` gives `decimal`. A minus sign in front of a number is part of it. A number that doesn't fit its type is rejected. | `55`, `-3`, `1.5`, `1e-3`, `5L`, `9.99m` |
| Boolean literal | `true` / `false`, in any case. | `true` |
| String literal | `"quoted string"`. Everything between the quotes is text, including `\| + - * / ( ) ,`. Write a `"` inside as `""`. | `"Hi"`, `"n/a"`, `"say ""hi"""` |
| Date/time literal | A constructor function taking one quoted string, read with the invariant culture and checked when the selector is parsed. A date-time keeps its kind (a trailing `Z` stays UTC). | `$FN_DT_DATETIME("2025-01-31T08:30:00")`, `$FN_DT_DATEONLY("2025-01-31")`, `$FN_DT_TIMEONLY("08:30")` |
| Arithmetic | `+ - * /`, unary `-`, parentheses. Operands are paths, literals or chains. | `Amount - 3 - 2` |

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
- **Declared type (`Type`) when the selector is parsed:** exact when every fallback is a literal. With a property
  fallback, arguments have no type yet, so `Type` is `null` until the expression is built. A string subject is the
  exception: it declares `string`. Code that builds the expression first, such as the QueryShaper mapper, still checks
  the result type. A function chained after a value-type coalesce with a property fallback (`DeletedAt|$FN_COALESCE(CreatedAt)|…`)
  can't be resolved in the generic form yet (roadmap: typed function arguments).
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

## Extending: custom functions

1. Inherit `FunctionPropertySelector<T>` (and the non-generic `FunctionPropertySelector`).
2. Expose public constructors `()` and `(List<PropertySelector>)`.
3. Decorate both with `[SelectorToken("$FN_ACME_MYFUNC")]`, using a vendor-scoped name.
4. At startup, call `SelectorRegistry.RegisterAssembly(typeof(MyFunc<>).Assembly)`.

Optionally call `SelectorRegistry.Initialize()` at startup, so the one-time assembly scan doesn't
land on the first request. Duplicate tokens throw.

## Metadata for UIs

`new PropertiesExplorer<T>().ExplorePropertiesTree(maxDepth)` returns a `SelectorsMetaDataTree`,
which a front end can use to offer column/field pickers.

## Internals map

| Area | Location |
|---|---|
| Core records, implicit `string` conversion | `PropertySelector.cs`, `FunctionPropertySelector*.cs` |
| String pre-scan, arithmetic entry | `PropertySelectorUtils.cs`, `ArithmeticSelectors/` (tokenizer → parser) |
| Function dispatch | `Registration/SelectorRegistry.cs`, `SelectorTokenAttribute.cs` |
| Literals | `ValueSelectors/ValueSelectorFactory.cs` |
| Coalesce | `NullSelectors/` |
| Member access building | `ExpressionUtils.cs`, `SelectorImplementations/MethodInfos.cs` |
