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

## Grammar

This section is the specification of the selector language, **grammar 0.1**, shipped with PropertySelectors
`0.1.0-preview`. What it promises across versions is set by decision 032: for any selector that parses, whether it
parses, its value, its declared and built types, its `ToString()` round trip and the type and position of its errors
are contract; new functions and new meanings for text that's rejected today are additive. Both forms (generic and
non-generic) follow the same grammar.

### Syntax

In EBNF, with whitespace rules below. A selector is read in this order: an exact number literal first, then
arithmetic, then a chain (see "Which rule applies").

```ebnf
selector   = empty | number | arithmetic | chain ;
empty      = "" ;                                   (* the root itself *)

chain      = part , { "|" , step } ;
part       = function | literal | path ;
step       = function | path ;                      (* a literal can't follow "|" *)
path       = name , { "." , name } ;
name       = [ "@" ] , identifier ;                 (* matched exactly against a public instance property *)
identifier = ( letter | "_" ) , { letter | digit | "_" | mark | connector | format } ;
                                                    (* a C# identifier; Unicode letters count *)
function   = token , [ "(" , [ argument , { "," , argument } ] , ")" ] ;
token      = "$" , { character - "(" } ;            (* built-ins and registered tokens start with "$FN_" *)
argument   = selector - empty ;                     (* resolved against the scope root *)

literal    = number | boolean | string ;
number     = [ "-" ] , digits , [ "." , digits ] , [ ( "e" | "E" ) , [ "+" | "-" ] , digits ] ,
             [ "L" | "l" | "m" | "M" ] ;
digits     = digit , { digit } ;
boolean    = "true" | "false" ;                     (* any case *)
string     = '"' , { character - '"' | '""' } , '"' ;

arithmetic = sum ;
sum        = product , { ( "+" | "-" ) , product } ;
product    = unary , { ( "*" | "/" ) , unary } ;
unary      = "-" , unary | primary ;
primary    = group , { "|" , step } | operand ;
group      = "(" , sum , ")" ;
operand    = chain ;                                (* with no "+ - * /" outside quotes and parentheses *)
```

Date and time literals are functions (`$FN_DT_DATETIME("…")`), so they need no rule of their own.

### Which rule applies

1. **A selector that is exactly a number literal is that literal**, so `-3` and `1e-3` aren't arithmetic.
2. **A selector is arithmetic** when it has a `+`, `-`, `*` or `/` outside quoted strings and parentheses, or when
   it starts with `(`. `$FN_DT_ADDDAYS(Created, -3)` isn't arithmetic: the `-` is inside parentheses.
3. **Otherwise it's a chain**, split at every `|` outside quoted strings and parentheses. Each part is, in order:
   a **function** if it starts with `$` (an unregistered token is "Unknown function", never a property); a
   **literal** if it's a whole string, boolean or number literal; otherwise a **path**. A leading `@` on a name
   makes it a path segment even when it reads as a literal (`@True`).
4. **A literal can only start a chain** (decision 033). After `|` it's rejected, because it would discard the value
   before it: `Age|5` and `Name|"x"` are "A literal can't follow '|': it would discard the value before it.",
   positioned at the literal. At the head it can be piped as usual (`5|$FN_MATH_SQRT`, `"ab"|$FN_STR_LENGTH`), and
   a property named like a literal is reached with `@` (`Inner|@True`).
5. **A group can only start a selector.** `(Age + 1)|$FN_MATH_ROUND` is allowed; `Age|(Age + 1)` is rejected.

### Precedence and associativity

| Binds | Construct | Notes |
|---|---|---|
| tightest | `.` | member access inside one path |
| | `\|` | `A + B\|$FN_X` is `A + (B\|$FN_X)`; `-Age\|$FN_MATH_SQRT` is `-(sqrt(Age))` |
| | unary `-` | `--3` is `3` |
| | `*` `/` | left-associative |
| loosest | `+` `-` | left-associative: `Amount - 3 - 2` is `(Amount - 3) - 2` |

A `-` directly before a number, where an operand is expected, is part of the number: `2 * -3` and `- 3` hold the
literal `-3`. Anywhere else, a `-` where an operand is expected is negation.

### Whitespace

Allowed, and ignored, at both ends of a selector, around `|`, around operators, around `,` and inside a group's
parentheses. Not allowed inside a name, a number, a token, or between a token and its `(`:
`$FN_MATH_MAX (Age, 1)` is "No space is allowed between a function and its '(': write '$FN_MATH_MAX('.", at the space, also inside arithmetic and arguments. A selector made only of whitespace is rejected
(it's an empty part), while the empty string is the root itself.

### Functions and arguments

- The `(` after a token opens the arguments, and the **last** `)` of the part closes them. Nothing but whitespace
  may follow it: `$FN_MATH_MAX(Age, 1)x` is "Unexpected 'x' after the closing ')'"; `$FN_ELM_FIRST.Number` and
  `$FN_ELM_FIRST(…).Number` are "A member can't follow a function with '.'" (use `|`).
- Arguments are split at `,` outside quoted strings and parentheses. An empty argument (`f(a,,b)`) is rejected.
  Empty parentheses are the same as none: `Items|$FN_AGGR_COUNT()` ≡ `Items|$FN_AGGR_COUNT`.
- Each argument is a full selector, resolved against the scope root (decision 031), except the ordering functions,
  whose arguments resolve against the collection's element. The piped value is the first argument (decision 016).

### Size and depth

A selector longer than `SelectorLimits.MaxLength` (default 2,000 characters) or deeper than `SelectorLimits.MaxDepth`
(default 32) is rejected before it's built; see [Limits](#limits).

### Names

A path is checked when the selector is parsed, in both forms, before any property is looked up:
- Each name is an `identifier` (above), optionally escaped with `@`. Anything else is rejected at the character:
  `Age#` is "Unexpected character '#' in a property name.", `Inner . Value` is "A property name can't contain
  whitespace.", `Age()` is "'(' can't follow a property name: only a function ('$FN_…') takes arguments.", and
  `Inner.1st` is "A property name can't start with a digit."
- An empty name (`.Age`, `Inner.`, `Age..Age`) is "Empty property name: '.' must sit between two names."
- A part that starts like a number but isn't one is a malformed number, not a name: `1.` and `.5` are "'1.' isn't a
  valid number. A '.' needs a digit on both sides ('0.5', '1.0').", `1e` names the missing exponent digits, and
  `0x10` or `1_000` are "'0x10' isn't a valid number."

### Constructs

| Construct | Form | Example |
|---|---|---|
| Property path | `Name(.Name)*`. Any public instance property works, including properties of value types, so date parts are plain paths. Fields are not members, in either form, so a `ValueTuple`'s `Item1` or a struct's public field can't be selected. On a nullable struct, a member that `Nullable<T>` doesn't have is read from `T` and the result is lifted to nullable: `Deleted.Year` is `int?`, and it's null when `Deleted` is (it builds `x.Deleted == null ? null : (int?)x.Deleted.Value.Year`). `Value` and `HasValue` keep their usual meaning. | `Address.City`, `Created.Year`, `Deleted.Year` |
| Pipe / chain | `part\|part\|…`. A pipe separates two selectors. Each part applies to the previous result: it's resolved against the piped value's type exactly as if that type were the root. A property after a function goes after a pipe, because `.` is only for navigation inside a path. See [Dot and pipe](#dot-and-pipe). | `Phones.Id\|$FN_AGGR_SUM`, `Phones\|$FN_ELM_FIRST\|Number`, `Created\|$FN_DT_ADDDAYS(3)\|Year` |
| Function | `$FN_<NAME>` or `$FN_<NAME>(arg, …)`. Arguments are selectors themselves, resolved against the scope root. A function gets its subject from the pipe or from its first argument; see [Piped and static forms](#piped-and-static-forms). | `$FN_STR_SUBSTRING(0, 3)`, `Price\|$FN_MATH_ROUND(2)` |
| Number literal | Read with the invariant culture on every machine (decision 017). An integer is `int`, or `long` if it doesn't fit. A `.` or an exponent makes it `double`. The suffix `L` gives `long` and `m` gives `decimal`. A minus sign in front of a number is part of it. A number that doesn't fit its type is rejected. | `55`, `-3`, `1.5`, `1e-3`, `5L`, `9.99m` |
| Boolean literal | `true` / `false`, in any case. A property with one of these names is written with the `@` escape (`@True`). | `true` |
| String literal | `"quoted string"`. Everything between the quotes is text, including `\| + - * / ( ) ,`. Write a `"` inside as `""`. | `"Hi"`, `"n/a"`, `"say ""hi"""` |
| Date/time literal | A constructor function taking one quoted string, read with the invariant culture and checked when the selector is parsed. A date-time keeps its kind (a trailing `Z` stays UTC). | `$FN_DT_DATETIME("2025-01-31T08:30:00")`, `$FN_DT_DATEONLY("2025-01-31")`, `$FN_DT_TIMEONLY("08:30")` |
| Arithmetic | `+ - * /`, unary `-`, parentheses. Operands are paths, literals or chains, and `\|` binds tighter than every operator (`A + B\|$FN_X` is `A + (B\|$FN_X)`), unary `-` included (`-4\|$FN_MATH_SQRT` is `-(sqrt(4))`). Any operand can be piped, a literal too (`Age + 2\|$FN_MATH_POW(3)`, `Age + "ab"\|$FN_STR_LENGTH`), and whitespace is allowed around `\|`. A parenthesized group can start a chain (decision 026), and a selector wrapped entirely in parentheses is arithmetic too. Operands must be numeric; dates and durations go through the date functions (decision 027). Both operands are widened to their common type, which is the result type (`Quantity + 1` → `int`, `Price * 3` → `decimal`, nullable if either operand is). | `Amount - 3 - 2`, `(Age + 1)\|$FN_MATH_ROUND` |

**Reserved character.** `:` is reserved outside quoted strings (decision 031), for a possible way to name a chain
value later. An unquoted `:` anywhere, in either form, is a `SelectorParseException`: "':' is reserved; write it
inside a quoted string to use it as text." Inside quotes it's ordinary text (`"a:b"`, `$FN_DT_TIMEONLY("08:30")`).

**Reserved prefix.** A part that starts with `$` is always a function (decision 030.3), never a property lookup. If no
function is registered under that token, it's "Unknown function '…'", in either form and wherever the part appears
(head of a chain, after `|`, in arithmetic, as an argument). Tokens are case-sensitive; a token that matches a
registered one except for case says so: "Unknown function '$fn_str_trim'. Function names are case-sensitive:
'$FN_STR_TRIM'."

**Case.** Property names are case-sensitive in both forms (decision 030.2): `age` doesn't find `Age`, and two
properties that differ only by case (`Code`, `code`) are two properties. A name that matches a public property
except for case says so: "Property 'age' was not found on type 'Item'. Property names are case-sensitive: 'Age'."
Only public instance properties are members; a property hidden with `new` resolves to the most derived one.

**Escaping a name.** A leading `@` on a name segment makes it a property name, never a literal (decision 030.4), as
with C# verbatim identifiers. It's needed only for a property named like a boolean literal (`True`, `false`), which
would otherwise be read as `true`/`false`; on any other name it's allowed and changes nothing (`@Age` ≡ `Age`). It
works on any segment and anywhere a property can appear: `@True`, `Inner.@True`, `Inner|@True`, `@True + 1`,
`$FN_MATH_MAX(@True, 1)`. `ToString()` keeps it. A `@` with no name after it (`@`, `@@True`, `Inner.@`) is "'@' must
be followed by a property name."; an unknown escaped name is reported without the `@` ("Property 'Missing' was not
found", positioned at the name).

`SelectorLiteral.Format(value)` writes any of these literals from a .NET value (`int`, `long`, `double`, `float`,
`decimal`, `bool`, `string`, `DateTime`, `DateOnly`, `TimeOnly`). Use it instead of building literal text by hand.
Values typed by end users should travel as filter values (QueryShaper's `ConditionNode.Value`), not inside selector text.

All tokens are constants on `SelectorDefaults`. Prefer those constants to string literals in your code.

### Dot and pipe

`.` and `|` mean different things (decision 022):

- **`.` navigates** inside one property selector: `Address.City`, `Phones.Number`, `Created.Year`. It's plain member
  access, with projection over collections and lifting over nullable structs, so it always translates.
- **`|` separates two selectors.** You need it wherever the next step isn't a member of the previous one: a function
  (`Phones|$FN_AGGR_COUNT`), or a property after a function (`Phones|$FN_ELM_FIRST|Number`,
  `Created|$FN_DT_ADDDAYS(3)|Year`, `Nickname|$FN_COALESCE(Name)|Length`).
- **A selector after `|` is resolved against the piped value**, exactly as it would be against a root of that type.
  So `Address|City` builds the same as `Address.City`, and a collection piped into a property projects:
  `Phones|Number` builds `Phones.Number`. Both are allowed, but `.` is the preferred way to write navigation.
- **At the start of a part, a collection's own members come first** (decision 023). A property is looked up on the
  value itself, and only if it isn't there, on its elements, which projects. So on an `IGrouping<K, T>`, `Key` is the
  grouping's key and `Quantity` projects the rows. On a piped `string[]`, `Names|Length` is the array's length, and
  `Phones|Count` is the list's `Count`. **Inside a dotted path**, a collection always projects: `Names.Length` is
  each name's length (`IEnumerable<int>`). So when a collection and its elements have a member with the same name,
  `Names|Length` and `Names.Length` mean different things. To count elements, prefer `$FN_AGGR_COUNT`.
- **`.` after a function is rejected**, and the message shows the piped form. `$FN_ELM_FIRST.Number` gives
  "A member can't follow a function with '.'. Write it after a pipe: '$FN_ELM_FIRST|Number'.", and
  `$FN_DT_ADDDAYS(3).Year` gives the same message with `'$FN_DT_ADDDAYS(3)|Year'`.
- Function **arguments** follow a different rule: they resolve against the scope root, not the piped value
  (see [Piped and static forms](#piped-and-static-forms)).

## Function catalog

| Family | Tokens |
|---|---|
| Aggregation | `AGGR_COUNT`, `AGGR_LONG_COUNT`, `AGGR_COUNTDISTINCT`, `AGGR_SUM`, `AGGR_AVG`, `AGGR_MIN`, `AGGR_MAX` |
| Element | `ELM_FIRST`, `ELM_LAST`, `ELM_ATINDEX` |
| Ordering | `ORDER_ASC`, `ORDER_DESC`, `THEN_ORDER_ASC`, `THEN_ORDER_DESC` |
| Math | `MATH_CEILING`, `FLOOR`, `ROUND`, `TRUNCATE`, `SIGN`, `POW`, `SQRT`, `EXP`, `LOG`, `LOG10`, `SIN`, `COS`, `TAN`, `ASIN`, `ACOS`, `ATAN`, `ATAN2`, `MAX`, `MIN` |
| String | `STR_LENGTH`, `TOUPPER`, `TOLOWER`, `TRIM`, `TRIMSTART`, `TRIMEND`, `SUBSTRING`, `INDEXOF`, `REPLACE`, `CONTAINS`, `STARTSWITH`, `ENDSWITH`, `CONCAT`, `JOIN`, `COMPARE`, `ISNULLOREMPTY`, `ISNULLORWHITESPACE` |
| Date | `DT_DATETIME`, `DATEONLY`, `TIMEONLY` (literals), `DT_ADDYEARS`, `ADDMONTHS`, `ADDDAYS`, `ADDHOURS`, `ADDMINUTES`, `ADDSECONDS`, `ADDMILLISECONDS`, `NOW`, `UTCNOW`, `TODAY`. The family is meant to grow (decision 017.9; roadmap). |
| Null handling | `COALESCE` |

Each `DT_ADD*` accepts only the date types that have that method: `ADDYEARS`, `ADDMONTHS` and `ADDDAYS` take `DateTime`,
`DateTimeOffset` and `DateOnly`; `ADDHOURS` and `ADDMINUTES` take `DateTime`, `DateTimeOffset` and `TimeOnly`;
`ADDSECONDS` and `ADDMILLISECONDS` take `DateTime` and `DateTimeOffset`. `THEN_ORDER_*` needs an ordered collection, so
it follows `ORDER_ASC` or `ORDER_DESC`. Both are checked when the selector is parsed (decision 035).

**Aggregate result types** follow `Enumerable`'s overload for the element type, and the selector's `Type` matches
the expression it builds. `AGGR_MIN`, `AGGR_MAX` and `AGGR_AVG` first lift a non-nullable value element to its
nullable form (`Numbers.Select(x => (int?)x).Max()`), so **an empty collection gives `null`**, both in memory and
in SQL (decision 014):

| Function | Element type → result |
|---|---|
| `AGGR_COUNT`, `AGGR_COUNTDISTINCT` | `int` (`AGGR_LONG_COUNT`: `long`). `AGGR_COUNTDISTINCT` counts distinct **non-null** values, like SQL's `COUNT(DISTINCT x)`, so `[1, null, 1, 2]` gives 2 in memory and in SQL (decision 020). |
| `AGGR_SUM` | the element type: `int` → `int`, `decimal?` → `decimal?`, … An empty collection sums to 0. |
| `AGGR_MIN`, `AGGR_MAX` | the nullable element type: `int` → `int?`, `decimal?` → `decimal?`, … |
| `AGGR_AVG` | `int`, `long`, `double` → `double?`; `float` → `float?`; `decimal` → `decimal?` (nullable elements give the same) |
| `AGGR_MIN`, `AGGR_MAX` on other comparable types | `DateTime` → `DateTime?` (any value type is lifted); reference types such as `string` stay as they are, through the generic overload |

`AGGR_SUM` and `AGGR_AVG` support the numeric types `int`, `long`, `float`, `double` and `decimal`, and their nullable
forms. Anything else is rejected by their signature, which takes "a collection of numbers", with the collection type as
written: "$FN_AGGR_AVG must be applied to a collection of numbers, not a List<TimeSpan>: '$FN_AGGR_AVG'." (or
`List<TimeSpan?>` for a nullable element). It's a `SelectorParseException` in both forms: when parsed in the generic
form, and when built in the non-generic form.

**Math result types.** `ROUND`, `CEILING`, `FLOOR` and `TRUNCATE` use the `Math` overload for the operand type when
there is one, and `double` otherwise (`CEILING` on an `int` → `double`). **On a `decimal` they are exact** and return
`decimal` (decision 024): `Price|$FN_MATH_ROUND(3)` on `0.5015m` is `0.502m`. They translate exactly on SQL Server and
PostgreSQL. EF's SQLite provider can't translate them; register QueryShaper's `UseDecimalMathAsDouble()` there (see
`query-shaper.md`), which runs them through `REAL`. The other math functions return `double`, and `SIGN` returns
`int`.

**`MAX` and `MIN`** (decision 025) pick the larger or smaller of two values in the same row. For the largest value
across a collection, use `AGGR_MAX`/`AGGR_MIN`. They're built as a conditional (`a >= b ? a : b`), not as a call to
`Math.Max`, so they translate on every provider: as `CASE WHEN … END` in SQL, including SQL Server before 2022, which
has no `GREATEST`. On SQLite a `decimal` comparison stays exact (EF's `ef_compare`).
- **Type:** the operands' common type, widened as in arithmetic: `MAX(int, long)` → `long`, `MAX(Price, 1)` → `decimal`,
  `MAX(decimal, double)` → `decimal`. Decimals are exact.
- **Nulls are ignored:** `MAX(null, 5)` and `MAX(5, null)` are 5. The result is null only when both operands are, so it's
  nullable only when both operand types are (`MAX(int?, int)` → `int`, `MAX(int?, int?)` → `int?`). To get null whenever
  either is null, coalesce first, or test for null separately.
- **Cost:** each operand appears twice in the SQL, and a nullable one three times. That's free for columns, but an operand
  that is itself a subquery (`Reviews|$FN_AGGR_COUNT`) may be computed more than once per row.
- In memory, `NaN` isn't propagated the way `Math.Max` does it: `MAX(NaN, 1)` is 1. Note that SQL and .NET round halves differently: SQLite rounds
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
  It's rejected with `SelectorParseException`: "…can't be called with 2 arguments besides the piped value… The piped value
  is its first argument, so 'Inner|$FN_MATH_MAX(…)' means '$FN_MATH_MAX(Inner, …)'."
- **Functions without a subject reject the pipe** when the selector is parsed: "$FN_DT_NOW takes no piped value, but
  'Created' is piped into it. Write it at the start of a chain: '$FN_DT_NOW'." `STR_JOIN` is rejected too, because its
  first argument is the separator, so `Tags|$FN_STR_JOIN(", ")` would mean `string.Join(Tags, ", ")`. Pass the value
  after the separator instead: `$FN_STR_JOIN(", ", Name, Nickname)`.
- **Declared type:** in the generic form the subject's type comes from the chain, so a piped function knows it when
  the selector is parsed (`Day|$FN_DT_ADDDAYS(3)` declares `DateOnly`). Since decision 018 the static form does too,
  because its arguments are typed: `$FN_DT_ADDDAYS(Day, 3)` declares `DateOnly`, and `$FN_MATH_MAX(A, B)` the wider
  of the two types.
- **The chain moves the subject; the arguments stay on the root** (decision 031). Each `|` step works on the value
  before it, but a function's arguments are always read from the scope root, the query's row type. On `AppPerson`,
  `Phones|$FN_ELM_FIRST|Id|$FN_MATH_MIN(Id)` is `Math.Min(p.Phones.First().Id, p.Id)`: the subject is the phone's `Id`,
  and the argument is the person's. To use a value from the chain, put it in the chain (`…|Id|$FN_MATH_MIN(5)`). The
  ordering functions are the exception: their arguments read from the collection's element. (`:` is reserved
  for a possible way to name a chain value in an argument; see the roadmap, Phase 6.)
- **Wherever the chain starts**, arguments resolve against the scope root: after a literal or a function without a
  subject too (decision 030.5). `5|$FN_MATH_MAX(Age)` builds `Math.Max(5, x.Age)`, and `$FN_DT_NOW|$FN_DT_ADDDAYS(Days)`
  builds `DateTime.Now.AddDays(x.Days)`. Inside an ordering function's argument, the scope root is the element.
  The root is the target that `ExpressionUtils.CreateMemberAccess` was given, so a caller that invokes a selector's
  `PropertyAccess` directly, without it, still gets the root only from the piped value.
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
  rejected, including a non-nullable piped value (`Quantity|$FN_COALESCE(0)` on an `int`), with a
  `SelectorParseException`: when parsed, or when built if the non-generic form only finds out then.
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
  fails then and not when the selector is parsed. It fails with the same `SelectorParseException` and message as
  the generic form.

## Errors

Anything wrong with the selector text is a `SelectorParseException`, which derives from `ArgumentException`
(decision 021). Callers such as an HTTP endpoint can catch that one type and answer 400. It carries:

| Property | Meaning |
|---|---|
| `Detail` | What is wrong, without the position: "Property 'Missing' was not found on type 'Order'." |
| `Selector` | The text the position refers to: the whole selector when it's parsed. |
| `Position` | 0-based offset into `Selector` of the token at fault, or `null` if it isn't known. |
| `Token` | The text at fault: a property name, a function name, an operator, an argument. |
| `Message` | `Detail` plus "Position N in '…'.", with the token added when `Detail` doesn't already name it. |

```
"Lines|Missing"                              → Property 'Missing' was not found on type 'Line'. Position 6 in 'Lines|Missing'.
"$FN_MATH_MAX(Amount, $FN_MATH_SQRT(Missing))" → position 35, inside the nested argument
"Amount * 2 3"                               → Unexpected '3': an operator is missing before it. Position 11 in 'Amount * 2 3'.
"Name|$FN_NOPE(1)"                           → Unknown function '$FN_NOPE'. Position 5 in 'Name|$FN_NOPE(1)'.
```

- **When the selector is parsed:** syntax errors (unbalanced parentheses, an unterminated string, an empty part such
  as `A||B`, an empty argument such as `f(a,,b)`, text after a function's closing `)`, an unexpected character or
  leftover operand in arithmetic), unknown functions, unknown properties in the generic form, invalid literals,
  argument count and type errors, ambiguous overloads and rejected pipes.
- **Positions** point at the token for syntax, property and arithmetic errors, at the argument for errors inside an
  argument, at the function for count and type errors, and at the operator for a non-numeric operand.
- **A non-numeric arithmetic operand** is "'+' needs numeric operands: '"a"' is String." (or "'-' needs a numeric
  operand: …" for negation), at the operator. It's found when the selector is parsed whenever the operand's type is
  known there: always for literals, and for every operand in the generic form (`Name + 1`, `Created * 2`,
  `Deleted + 1` → `DateTime?`).
- **When the expression is built**, in the non-generic form: an unknown property is a `SelectorParseException` too,
  with the generic form's message. `Selector` is then the property path and `Position` an offset into it, because
  the full text isn't known at that point. A non-numeric property operand is found here too, with `Selector` the
  operation's canonical text (`ToString()`) and `Position` its operator: `Age + Name * 2` reports
  `'Name * 2'`, position 5. A function's type mismatch is found here too, with the generic form's detail: `Selector`
  is the function as written with its piped value when that value is an argument (`Name|$FN_MATH_SQRT`, position 5),
  or the function alone when it's the target (`true|$FN_STR_LENGTH` → `'$FN_STR_LENGTH'`, position 0).

## Limits

Selectors usually arrive from clients, so parsing has two limits (decision 030). Both are checked when the selector
is parsed, and going over either is a `SelectorParseException`:

| Setting | Default | Meaning |
|---|---|---|
| `SelectorLimits.MaxLength` | 2,000 | Characters in the whole selector. Checked before anything else is read. |
| `SelectorLimits.MaxDepth` | 32 (at most `MaxDepthCeiling`, 256) | How deeply the selector nests. |

**Depth** is the depth of the expression the selector builds. A property path counts one level per segment
(`Next.Next.Age` is 3). A function, an arithmetic operation and a negation each count one, plus their deepest operand.
A chain step adds to what it receives: `Name|$FN_STR_TRIM` is 2, and `Age|$FN_MATH_MAX(Next.Age)` is 3. So a flat sum
of 33 terms is too deep, because each `+` wraps the sum before it. Parentheses count one level each, even though they
add nothing to the expression.

Without these limits, a long or deeply nested selector would overflow the stack and end the process, which .NET can't
catch. The limits are global settings, read on every parse. Raising them is allowed, up to the ceiling for depth. A
limit error has no `Position`, and its `Selector` is the whole text.

```
"$FN_MATH_SQRT(" × 32 + "Age" + ")" × 32  → The selector is nested more than 32 levels deep (SelectorLimits.MaxDepth).
a 2,001-character selector               → The selector is 2001 characters long; the limit is 2000 (SelectorLimits.MaxLength).
```

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
3. Decorate both with `[SelectorToken("$FN_ACME_MYFUNC")]`, using a vendor-scoped name. Set `Category` so UIs can group it.
4. Declare the overloads in one static table, and implement `IFunctionSignatures` in both records with a static
   `Signatures` property that returns it (decisions 019 and 034). The registry rejects a function without it.
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
  `constraint.Accepts(type)` tells whether a value of that type fits (a literal-only constraint accepts none; check
  `IsLiteral`).
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
value ("the piped value 'Name' must be a number") or the target ("$FN_AGGR_SUM must be applied to a collection of
numbers, not a String"). Both are `SelectorParseException`s when the selector is parsed (see "Errors"). In the
non-generic form, where a type is unknown until the expression is built, the signature is resolved again then with
the built types, so a type error found only then is a `SelectorParseException` with the same detail.

Every function has a table. Because `Signatures` is static, the table can be read without creating the selector,
which is what the function metadata for UIs is built on. A selector resolved from text exposes the overload it
picked as `Signature`.

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
which a front end can use to offer column/field pickers. It lists what a selector path can reach, by the parser's
rules:

- Each node has its `Path` (the selector text, `""` for the root, with `@` added to a name that reads as a literal,
  such as `Phones.@True`) and its `SelectorType`, which is the type that path declares: `Deleted.Year` is `int?` and
  `Phones.Number` is `IEnumerable<string>`.
- Under a collection the children are the element's properties (`Phones.Number`), never the collection's own
  (`Count`, `Capacity`). Under a nullable value they are the underlying type's (`Deleted.Year`, not `HasValue`).
  Indexers and fields aren't listed.
- A node whose type is a collection has `IsCollection` and its `ElementType`, which is where aggregates apply.
- A type already on the way down from the root ends as a leaf, so `Friends` on a `Person` is listed without children.
- `TypeName` and `ElementTypeName` give C#-style names (`int?`, `List<Phone>`) for a UI or a DTO;
  `TypeDisplay.Name(type)` formats any other type the same way.

Functions come from `FunctionCatalog` (namespace `Steverzag.PropertySelectors.MetaData`), which reads the registry, so
it includes functions registered from other assemblies:

- `FunctionCatalog.All` lists every registered function, ordered by token. `Find(token)` returns one, or `null`.
  Each `FunctionDescriptor` has the `Token`, the `Category`, the piped form (`Piped`, `PipedHint`), the argument scope
  (`Arguments`), which forms are registered (`HasGenericForm`, `HasNonGenericForm`) and the `Signatures`.
- `FunctionCatalog.After(type)` answers "what can follow a value of this type with `|`?". It returns one
  `FunctionMatch` per overload that accepts the type. A match carries the arguments still to write (`Parameters` and
  `Rest`, without the piped one) and the `ResultType` (with `ResultTypeName`). The result type is `null` when it depends on an argument,
  as for `MATH_MAX`. Functions that reject the pipe (`DT_NOW`, `STR_JOIN`) are never listed.

A UI combines the two: pick a node from the tree, ask `After(node.SelectorType)`, and repeat with the result type.
To offer values for an argument, filter the properties with `match.Parameters[i].Accepts(property type)`. A literal-only
argument (`IsLiteral`, as the index of `ELM_ATINDEX`) accepts no type: the UI asks for a literal instead.

```csharp
FunctionCatalog.After(typeof(decimal));            // MATH_ROUND() → decimal, MATH_ROUND(an integer) → decimal, MATH_MAX(a number) → ?, …
FunctionCatalog.After(typeof(List<int>));          // AGGR_SUM() → int, AGGR_AVG() → double?, ELM_FIRST() → int, ORDER_ASC() → IOrderedEnumerable<int>, …
FunctionCatalog.After(typeof(IOrderedEnumerable<int>)); // … and THEN_ORDER_ASC, THEN_ORDER_DESC
```

`After` follows the same rules as the parser: with valid arguments, a match parses and declares the result type it
reports. The
categories are `Aggregation`, `Element`, `Ordering`, `Math`, `String`, `Date` and `Null`, set with
`[SelectorToken(…, Category = FunctionCategory.Math)]`. A custom function that doesn't set one is `Other`.

## Internals map

| Area | Location |
|---|---|
| Core records, implicit `string` conversion | `PropertySelector.cs`, `FunctionPropertySelector*.cs` |
| String pre-scan, arithmetic entry | `PropertySelectorUtils.cs`, `ArithmeticSelectors/` (tokenizer → parser) |
| Function dispatch | `Registration/SelectorRegistry.cs`, `SelectorTokenAttribute.cs` |
| Function metadata | `MetaData/FunctionCatalog.cs`, `Signatures/IFunctionSignatures.cs` |
| Literals | `ValueSelectors/ValueSelectorFactory.cs` |
| Coalesce | `NullSelectors/` |
| Member access building | `ExpressionUtils.cs`, `SelectorImplementations/MethodInfos.cs` |
