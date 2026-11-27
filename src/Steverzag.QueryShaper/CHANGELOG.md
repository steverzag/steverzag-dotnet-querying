# Changelog: Steverzag.QueryShaper

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[SemVer](https://semver.org/) as set by decision 032: while the major is 0, a release that breaks anything bumps the
minor and an additive or fix-only release bumps the patch. Pre-releases (`0.1.0-preview.N`) may break each other.

Every break is listed under **Breaking**, whatever the version. Each release names the Steverzag.PropertySelectors
range it depends on. Releases are tagged `queryshaper-v<version>`.

## [0.1.0-preview] - 2026-11-27

First preview. Targets `net9.0`, which is out of support; the targets move at 1.0 (decision 037).
Depends on Steverzag.PropertySelectors `[0.1.0-preview, 0.2.0-a)`.

### Added

- **Client requests:** `QueryRequest` (search, filters, order, page), which binds from JSON with no setup, and the
  injected `IQueryShaper` (`Apply`, `ApplyPaged`, `ApplySearch`, `ApplyFilters`, `ApplyOrder`, `Map`,
  `ResolvePage`). Every error in a request is a `QueryRequestException` with the `Path` of the error.
  `condition`, `connector` and `direction` are read by name only, also when the app adds `JsonStringEnumConverter` to
  its JSON options.
- **Guard rails** in `QueryShaperOptions`: `DefaultPageSize` and `MaxPageSize`, `MaxFilters`, `MaxListValues`,
  `MaxOrders`, `MaxSearchLength`, a `MemberFilter` allow-list, and `UnsortableTypes`. A hidden member is reported as
  "not found", so a client can't tell it exists.
- **Filtering:** a `FilterNode` tree with And/Or groups and 20 conditions (comparisons, text, emptiness, `In`/`NotIn`,
  and `Contains*` over collections). Client values are sent as SQL parameters, so EF Core compiles one query per shape.
- **Base search** over the string lookups configured per type (`ConfigureSearch<T>`). It ignores case by default;
  `caseSensitive: true` opts out. On SQLite only ASCII letters fold.
- **Ordering, pagination, grouping and aggregates:** `ApplyOrder`, `Paginate`, `ApplyGrouping`, `ApplySelect`.
- **Stable pages:** `ConfigureDefaultOrder<T>` orders a request that has no order, and breaks ties after one that does.
  `ApplyPaged` throws `InvalidOperationException` when nothing orders the rows, instead of paging in an undefined order.
- **Configuration:** `QueryShaperConfiguration`, `AddQueryShaper<T>()`, and selector-based maps
  (`AddMap<TIn, TOut>`, `ForMember`).
- The extension methods (`ApplyLogicsFilter`, `ApplyBaseFilter`, `ApplyOrder`, `ApplySelect`, `Paginate`) are for
  trusted input built in code, and have no guard rails.
