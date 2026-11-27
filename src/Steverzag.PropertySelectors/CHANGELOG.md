# Changelog: Steverzag.PropertySelectors

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[SemVer](https://semver.org/) as set by decision 032: while the major is 0, a release that breaks anything bumps the
minor and an additive or fix-only release bumps the patch. Pre-releases (`0.1.0-preview.N`) may break each other.

Each release lists **Grammar** changes (what selector text means, the contract in 032.2) separately from **API**
changes (C# types and members). Every break is listed under **Breaking**, whatever the version. Releases are tagged
`propertyselectors-v<version>`.

## [0.1.0-preview] - Unreleased

First preview. Targets `net9.0`, which is out of support; the targets move to every supported .NET at 1.0
(decision 037).

### Grammar 0.1

- Property paths with nested navigation and collection projection (`Address.City`, `Phones.Number`), and `@` to
  read a name that would otherwise parse as a literal (`@True`).
- Literals: numbers with `L` and `m` suffixes, booleans, strings, and the date literals `$FN_DT_DATETIME`,
  `$FN_DT_DATEONLY` and `$FN_DT_TIMEONLY`.
- Arithmetic (`+ - * /`, unary `-`, parentheses) over numeric operands, with C#-style widening.
- Functions, chained with `|`. Every function with a subject accepts the piped form, where the piped value is the
  first argument (`Price|$FN_MATH_ROUND(2)`); arguments resolve against the scope root.
  - Aggregation: `AGGR_COUNT`, `AGGR_LONG_COUNT`, `AGGR_COUNTDISTINCT`, `AGGR_SUM`, `AGGR_AVG`, `AGGR_MIN`,
    `AGGR_MAX`. `MIN`, `MAX` and `AVG` are nullable, so an empty collection gives `null`.
  - Element: `ELM_FIRST`, `ELM_LAST`, `ELM_ATINDEX`.
  - Ordering: `ORDER_ASC`, `ORDER_DESC`, `THEN_ORDER_ASC`, `THEN_ORDER_DESC`.
  - Math (19 functions). `ROUND`, `CEILING`, `FLOOR` and `TRUNCATE` are exact on `decimal`; `MAX` and `MIN` compare two
    values in a row and translate on every provider.
  - String (17 functions).
  - Date: `DT_ADD*` for years through milliseconds, `DT_NOW`, `DT_UTCNOW`, `DT_TODAY`.
  - Null handling: variadic `COALESCE`.
- Reserved for later meaning: a part that starts with `$` is always a function, never a property, and an unquoted
  `:` is rejected anywhere.
- Limits: 2,000 characters and a nesting depth of 32 by default (`SelectorLimits`).

### API

- `PropertySelector<T>` (generic, typed when parsed) and `PropertySelector` (non-generic), built from a string by
  an implicit conversion, with `PropertyAccess` to build the expression and a `ToString()` that round-trips.
- `SelectorParseException`, with the `Position` and `Token` of the error in the whole selector.
- `SelectorRegistry` and `[SelectorToken]`: custom functions from other assemblies, declared with signature tables.
- Metadata for UIs: `PropertiesExplorer<T>` and `SelectorsMetaDataTree` (the reachable paths and their types), and
  `FunctionCatalog` (the registered functions and which can follow a given type).
- `MemberFilter` (`All`, `Portable`, `Except`, `Only`): one rule that hides members from the metadata tree and
  rejects them when a selector is parsed.

### Known limitations

- `TimeOnly` parts (`OpensAt.Hour`) work in memory but don't translate on EF Core's SQLite provider.
- Function arguments are typed only in the generic form.
