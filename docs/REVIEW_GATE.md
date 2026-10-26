# Architecture Refactor — Review Gate

Use this as a **final review after Claude finishes**. A claimed pass is not a verified pass until the command and output have been inspected.

## Baseline and migration safety
- [ ] Existing work was done on a dedicated branch or reversible commit.
- [ ] Migration 01 schema, temporal history, constraints, indexes and seed behavior unchanged unless a specific reviewed change was approved.
- [ ] `git diff --check` is clean; no accidental generated files or secrets committed.

## Module boundaries
- [ ] Each module's Domain is independent of EF Core, HTTP and Infrastructure.
- [ ] Application depends inward and contains concrete feature handlers/validators.
- [ ] Other modules communicate through Contracts, not direct entity access.
- [ ] Shared ErpDbContext remains an implementation detail and module boundaries are enforced in architecture tests.
- [ ] A deliberately illegal cross-module dependency is detected by tests (then removed).

## Runtime functionality
- [ ] At least one real feature uses Result<T> + FluentValidation + error mapping end-to-end.
- [ ] Expected failures return stable code + appropriate HTTP status; unexpected failures become sanitized ProblemDetails.
- [ ] Invalid required Options fail at startup; settings are not scattered throughout services.
- [ ] Serilog uses structured events and redacts sensitive values; module code depends on ILogger<T>.
- [ ] API starts and reports health/readiness, and DI resolves module contracts.

## Tests and delivery
- [ ] `dotnet build` succeeds on the intended SDK.
- [ ] Unit + integration + architecture tests pass; SQL Server-specific tests use SQL Server.
- [ ] Any skipped security audit is named with OS limitation, not counted as passed.
- [ ] OpenAPI, README and Architecture/ADR docs reflect actual implementation.
- [ ] Exact list of changed files, command outputs, known limitations and postponed work provided.
