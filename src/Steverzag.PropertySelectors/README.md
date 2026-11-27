# Steverzag.PropertySelectors

A small, dependency-free language that turns a string into a LINQ `Expression` that a query provider
(EF Core, LINQ to Objects) can translate.

Use it when a property, a computed value or an aggregate is chosen at runtime: sort columns sent by a UI, saved
report definitions, dynamic filters or mapping configuration. If the selection is known at compile time, a plain
lambda is the better tool.

```csharp
using Steverzag.PropertySelectors;

PropertySelector<Person> name  = "Name";                            // p => p.Name
PropertySelector<Person> city  = "Address.City";                    // p => p.Address.City
PropertySelector<Person> avgId = "Phones.Id|$FN_AGGR_AVG";          // p => p.Phones.Select(x => x.Id).Average()
PropertySelector<Person> calc  = "(Salary + 100) * 2";              // arithmetic
PropertySelector<Person> round = "Salary|$FN_MATH_ROUND(2)";        // Math.Round(p.Salary, 2)
PropertySelector<Person> since = "$FN_DT_DATEONLY(\"2024-01-01\")"; // date literal

Expression body = avgId.PropertyAccess(parameter);
```

It ships aggregation, element, ordering, math, string, date and coalesce functions; custom functions can be
registered from your own assemblies. Length and nesting depth are capped (`SelectorLimits`), so selectors from
untrusted clients can't exhaust the stack.

## Documentation

The grammar, the function catalog, errors, limits, extending and the metadata for UIs:
[docs/property-selectors.md](https://github.com/steverzag/steverzag-dotnet-querying/blob/main/docs/property-selectors.md).

## Status

`0.1.0-preview`: the API and the grammar (0.1) may still change before 1.0. The preview targets `net9.0`, which is
out of support; the targets move to every supported .NET at 1.0.

To filter, order, page and map whole queries from a client request, see
[Steverzag.QueryShaper](https://www.nuget.org/packages/Steverzag.QueryShaper).
