# ADR 0006 — Observability: Serilog in the host, log each failure once

Status: accepted (2026-10-26)

## Decision
* Serilog is configured only in the host (levels from the `Serilog` section; readable console in Development,
  compact JSON elsewhere). Modules log through `ILogger<T>` with `[LoggerMessage]` source-generated methods.
* Every request gets a correlation id (caller's `X-Correlation-ID` if short and plain, else the trace id), pushed
  to the log context, returned in the response header and in ProblemDetails. Request logging records method,
  path (no query string), status, elapsed time and user id.
* Commands log their outcome (name, duration, error code) through the logging decorator, never their payload;
  important use cases log a business event (warehouse created, product created/renamed).
* **Each unexpected exception is logged once**, at Error, by `GlobalExceptionHandler`. EF Core's own error events
  for failed commands, queries and saves are lowered to Debug: expected failures are already `Result` errors and
  unexpected ones are logged by the handler. Tests verify both (one Error per unexpected failure; none for a handled
  conflict).
* Never logged: credentials, tokens, connection strings, request bodies, query strings.
* Health: `/health/live` (process) and `/health/ready` (SQL Server via the DbContext check); `/api/health` kept.

## Deferred
OpenTelemetry tracing and metrics: activity ids already flow (traceId in logs and problems); exporters can be
added in `ObservabilityRegistration` without touching modules.
