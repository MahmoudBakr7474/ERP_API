# ADR 0007 — Configuration through validated options

Status: accepted (2026-10-26)

## Decision
* Required settings are strongly typed options bound from one section each and validated at startup
  (`ValidateDataAnnotations` + rules + `ValidateOnStart`): `Database` (connection string, command timeout),
  `Pagination` (default ≤ max page size), `Identity:Sessions` (lifetimes, idle ≤ absolute, access ≤ refresh ≤ absolute), `Identity:Password` /
  `Identity:Lockout` (length ≥ 8, lockout on), `Identity:Bootstrap` (both or neither) — ADR 0009.
  An invalid value stops the host before it serves a request (tested).
* Services receive `IOptions<T>`; nothing outside composition code reads `IConfiguration`.
* Secrets stay out of source control: the Development connection string is LocalDB with Windows authentication;
  other environments use environment variables (`Database__ConnectionString`) or a secret store.
* Business settings that change at runtime (document approval policy per DocumentType, Inventory) are **not**
  appsettings; they will be data owned by their module, editable and audited.
