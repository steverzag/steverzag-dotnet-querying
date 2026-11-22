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
