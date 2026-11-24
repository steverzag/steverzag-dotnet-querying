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

Expression body = avgId.PropertyAccess(parameter);
```

## Grammar (informal, not frozen yet; see roadmap Phase 4)

| Construct | Form | Example |
|---|---|---|
| Property path | `Name(.Name)*` | `Address.City` |
| Pipe / chain | `part\|part\|…`, where each part applies to the previous result | `Phones.Id\|$FN_AGGR_SUM` |
| Function | `$FN_<NAME>` or `$FN_<NAME>(arg, …)`. Arguments are selectors themselves. | `$FN_STR_SUBSTRING(0, 3)` |
| Literal | number, `true`/`false`, `"quoted string"` | `55`, `"Hi"` |
| Typed literal | `$VAL_<KIND>:<value>` with KIND ∈ INT, DOUBLE, BOOL, STR, DATETIME, DATE, TIME | `$VAL_DATE:2025-01-31` |
| Arithmetic | `+ - * /`, unary `-`, parentheses. Operands are paths or literals. | `Amount - 3 - 2` |

All tokens are constants on `SelectorDefaults`. Prefer those constants to string literals in your code.

## Function catalog

| Family | Tokens |
|---|---|
| Aggregation | `AGGR_COUNT`, `AGGR_LONG_COUNT`, `AGGR_SUM`, `AGGR_AVG`, `AGGR_MIN`, `AGGR_MAX` (`AGGR_COUNTDISTINCT` is declared but not implemented) |
| Element | `ELM_FIRST`, `ELM_LAST`, `ELM_ATINDEX` |
| Ordering | `ORDER_ASC`, `ORDER_DESC`, `THEN_ORDER_ASC`, `THEN_ORDER_DESC` |
| Math | `MATH_CEILING`, `FLOOR`, `ROUND`, `TRUNCATE`, `SIGN`, `POW`, `SQRT`, `EXP`, `LOG`, `LOG10`, `SIN`, `COS`, `TAN`, `ASIN`, `ACOS`, `ATAN`, `ATAN2`, `MAX`, `MIN` |
| String | `STR_LENGTH`, `TOUPPER`, `TOLOWER`, `TRIM`, `TRIMSTART`, `TRIMEND`, `SUBSTRING`, `INDEXOF`, `REPLACE`, `CONTAINS`, `STARTSWITH`, `ENDSWITH`, `CONCAT`, `JOIN`, `COMPARE`, `ISNULLOREMPTY`, `ISNULLORWHITESPACE` |
| Date | `DT_ADDYEARS`, `ADDMONTHS`, `ADDDAYS`, `ADDHOURS`, `ADDMINUTES`, `ADDSECONDS`, `ADDMILLISECONDS`, `NOW`, `UTCNOW`, `TODAY` |

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

## Generic vs non-generic

- `PropertySelector<T>` knows its root type, so it can resolve property types, function overloads
  and arithmetic on properties. **Use it whenever possible.**
- Non-generic `PropertySelector` exists for DTOs and serialization, where `T` isn't known yet.
  QueryShaper converts it to the generic form internally. Limitation: arithmetic involving properties
  only works on the generic form.

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
| Member access building | `ExpressionUtils.cs`, `SelectorImplementations/MethodInfos.cs` |
