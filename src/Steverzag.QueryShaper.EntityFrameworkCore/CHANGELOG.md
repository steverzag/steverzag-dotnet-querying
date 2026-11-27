# Changelog: Steverzag.QueryShaper.EntityFrameworkCore

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[SemVer](https://semver.org/) as set by decision 032: while the major is 0, a release that breaks anything bumps the
minor and an additive or fix-only release bumps the patch. Pre-releases (`0.1.0-preview.N`) may break each other.
From 1.0, the major follows EF Core's (decision 037).

Every break is listed under **Breaking**, whatever the version. Each release names the EF Core version it builds
against and the Steverzag.QueryShaper range it depends on. Releases are tagged
`queryshaper-entityframeworkcore-v<version>`.

## [0.1.0-preview] - Unreleased

First preview. Built against EF Core 9 (`Microsoft.EntityFrameworkCore` 9.0.10) on `net9.0`.
Depends on Steverzag.QueryShaper `[0.1.0-preview, 0.2.0-0)`.

### Added

- Async paging: `ApplyPagedAsync` on `IQueryShaper` for client requests, and `PaginateAsync` for queries built in
  code. When no default order is configured, `ApplyPagedAsync` orders by the entity's primary key, or breaks ties with
  it, so pages are stable with no setup.
- Provider presets for SQLite, SQL Server and PostgreSQL: `ProviderMemberFilters` (members that translate) and
  `ProviderUnsortableTypes`, each with `For(providerName)` and `For(dbContext)`.
- `UseDecimalMathAsDouble()`, a query interceptor for SQLite that lets `ROUND`, `CEILING`, `FLOOR` and `TRUNCATE` over
  a `decimal` translate, in `double` precision.
