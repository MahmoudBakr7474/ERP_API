# ADR 0012 — Identity completion: audit trail, credential limits, token rotation, scope guards, stable contract

Status: accepted (2026-10-27), on `feature/identity-completion`. Extends ADR 0009 and closes most of its *Deferred*
list. The product owner approved the decisions that change visible behavior on 2026-10-27; they are marked
**(approved)**. Two guards added by the pre-commit review are marked **(new)** and await the owner's confirmation.

## Context
The Identity sign-off review found that the ADR 0011 refactor changed no behavior, and it listed gaps that already
existed on `main`:
* management checks compared permissions but not scope;
* refresh-token reuse was recognized for one rotation only;
* the lockout count was a read-modify-write;
* password change ignored lockout;
* administrators could reset their own password through administration;
* role edits checked only the new permission set;
* there was no per-client limit, no audit trail, no session cleanup and no forwarded-headers setting;
* role and scope edits were last-write-wins;
* session times went out without a UTC marker;
* nothing guarded the OpenAPI contract.

The pre-commit review then found that an administrative password reset, and an unlock of a user with failed attempts,
always answered 409 on `main` as well: each UserManager step re-attached the tracked user and took the previous step's
unsaved stamp as the original value.

## Decision
1. **Audit trail.** `identity.SecurityEvents` is append-only and has no foreign keys. It is written through
   `ISecurityEventLog` in the use case's single save. The one exception is a refused sign-in or password change, whose
   count is committed first (see *Login transaction boundaries*).
   * Each row holds the actor, time, client IP, correlation id, the target user, role or session, and a small JSON
     detail without secrets.
   * It is read through `GET /api/identity/security-events`, which needs the new permission `identity.audit.read`.
2. **Credential limits (approved).** Each credential operation has its own fixed window per client address, so one
   busy operation never spends another's budget. A branch's POS refreshes can't keep its users from signing in.

   | Operation (`Identity:SignInRateLimit:<operation>`) | Endpoint | Default per minute |
   |---|---|---|
   | `Login` | `POST /login` | 60 |
   | `IssueApiToken` | `POST /tokens` | 60 |
   | `RefreshApiToken` | `POST /tokens/refresh` | 300 |
   | `ChangeMyPassword` | `POST /me/password` | 30 |

   * Each has `PermitLimit` (1–100000) and `Window` (1 s–1 h, default 1 min), read and validated once at startup: a
     change applies after a restart.
   * The client address is read after the trusted forwarded headers (decision 8). An IPv4-mapped address counts as
     its IPv4 address, and an IPv6 address counts as its /64: one IPv6 LAN segment (and every link-local client) is
     one budget, like a NAT address, while a client holding a larger prefix gets one budget per /64.
   * Sizing for branches whose users share one public address: about twice the clients that use the operation behind
     the busiest address. A head-office VPN that NATs every branch counts as one address. A refresh token is a 256-bit
     secret, so its limit only protects the server.
   * Separate budgets add up: one address gets `Login` plus `IssueApiToken` password checks per window. Lockout still
     bounds each account.
   * The host answers 429 with `Retry-After` and `Common.TooManyRequests` for every rate-limit policy.
3. **Lockout counting (approved: the ConcurrencyStamp correction).** A wrong password's count, the lockout it may
   start, and a right password's reset are written by atomic `UPDATE`s, not by the use case's save.
   * The first `UPDATE` applies only while the row still has the `ConcurrencyStamp` this request loaded. Then no other
     request changed it, the next count follows from the loaded one, and the request's tracked user takes the
     written values: as original values, and as current values unless it has a pending change of its own (a rehash's
     new stamp), which stays pending.
   * Otherwise two conditional `UPDATE`s count on whatever the row holds, and the tracked user stays as loaded.
   * Every `UPDATE` renews `ConcurrencyStamp`, so a user row loaded earlier and saved later (profile edit, activation,
     password change) answers 409 instead of writing back a stale count or lockout.
   * The `UPDATE`s ignore the request's cancellation: once a password has been checked, its outcome is written.
   * Password change answers `Identity.Password.LockedOut` (422) while the account is locked out. A wrong current
     password counts, and the one that starts a lockout records `UserLockedOut`.
   * A refused sign-in of an inactive user saves only its event; a pending rehash of the right password is dropped.
   * An administrative reset or unlock makes several UserManager steps; its single save is checked against the stamp
     it loaded, so it still refuses a change another request made.
4. **Refresh-token rotation (approved: strict).** Each exchanged refresh token's hash is kept in
   `identity.RetiredRefreshTokens`; the primary key is the hash, and rows cascade with their session.
   * Presenting any retired token of a live session ends the session (`Identity.Session.RefreshTokenReused`). Once a
     reuse is detected, ending the session doesn't depend on the client staying connected.
   * Two refreshes with the same token collide on the key; the losing request reloads the session
     (`IIdentityPersistence.DiscardChanges`) and ends it.
   * An ended session's tokens answer `Identity.Session.Revoked`, with no new event.
   * A refresh for a deactivated user's live session ends it and records `SessionRevoked`.
   * An honest double submit of one refresh token also signs the POS out. So the POS keeps one refresh in flight for
     all its callers, refreshes shortly before `accessTokenExpiresAtUtc` (not on every 401), never resends a refresh
     automatically (a refresh that timed out means signing in again), and treats `Identity.Session.RefreshTokenReused`
     or `Identity.Session.Revoked` as signed out.
5. **Scope and role guards.**
   * Managing a user requires the target's current scope to lie within the caller's own
     (`Identity.Access.ExceedsOwnScope`).
   * Granting Administrator, or managing its holders, requires `*`.
   * A role edit checks both the old and the new permission sets.
   * **(approved)** Self-service password changes need the current password. Administration can't reset the caller's
     own password (`Identity.User.CannotResetOwnPassword`, 422).
   * **(new)** Administration can't unlock the caller's own account (`Identity.User.CannotUnlockSelf`, 422): the
     lockout is what stops a stolen session from guessing the current password.
   * **(new)** The last active Administrator, and the last active company-wide one, can't lose the role or be
     deactivated, and the last company-wide one can't lose that scope (`Identity.User.LastAdministrator`). A
     branch-scoped Administrator can't manage company-wide users or grant company-wide scope, so nobody could bring
     that scope back.
   * Each such guarded change gives the Administrator role a new version in the same save, read before the counts, so
     two of them checked at the same time can't both commit.
   * Residual risk: an account that can create or re-role an equal peer can still reset the caller's password from
     it. The audit trail shows it (UserCreated or UserRolesChanged, then PasswordReset by another actor).
6. **Optimistic concurrency from the screen.**
   * `GET /roles/{id}` returns `version`, and `PUT /roles/{id}` requires it.
   * `GET /users/{id}` returns `access.revision`, and `PUT /users/{id}/access` takes it.
   * A mismatch answers 409 `Common.ConcurrencyConflict`. `PK_UserRoles` violations also map to 409.
7. **Session cleanup.** A hosted service deletes sessions that ended more than `RetentionAfterEnd` (30 d) ago, every
   `CleanupInterval` (1 h); their retired refresh tokens go with them.
8. **Transport (approved).**
   * Every credential endpoint, creating a user and resetting a password require HTTPS
     (`Identity.Session.HttpsRequired`, 403).
   * Tokens sent over plain HTTP are ignored.
   * HSTS is on outside Development, and token responses send `Cache-Control: no-store`.
   * The `ReverseProxy` options (off by default, validated at startup) trust forwarded headers from the listed
     proxies only. With them off, forwarded headers are not processed at all, even if the host's own
     `ASPNETCORE_FORWARDEDHEADERS_ENABLED` switch adds the middleware.
9. **UTC on the wire.** Identity timestamps are read back as UTC (a value converter, with no schema change).
10. **Stable OpenAPI contract (approved).**
    * `docs/api/openapi-v1.json` is committed and compared by `HostTests`. Running it with
      `ERP_UPDATE_OPENAPI_SNAPSHOT=1` rewrites the file and fails on purpose, so a variable left set can't accept
      later changes.
    * Schema names are the CLR type names, except that `OpenApiSetup` publishes a body-bound command as `...Request`
      (for example `CreateProductCommand` → `CreateProductRequest`, `CreateWarehouseCommand` →
      `CreateWarehouseRequest`). It refuses two types with one schema name and shows query parameters in camelCase.
    * Each Identity endpoint declares the statuses its handler can return. The ProblemDetails schema declares
      `errorCode` (in every problem body), `traceId` and `correlationId` (when the client accepts JSON) and
      `errors[]`.
    * Numbers are JSON numbers only (`JsonNumberHandling.Strict`), for every request body.

## Login transaction boundaries
No transaction spans a login. The count, a lockout and a reset commit first, each on its own. The security events,
the session and a rehashed password follow in the use case's single save. `LoginSaveFailureTests` and
`LockoutConcurrencyTests` verify this on SQL Server.

| What fails | What stays | Why this is intended |
|---|---|---|
| Saving the events of a counted failed attempt (or of a rejected password change) | The count and any lockout. The events are lost, and the client gets 500. | Fail-secure: making the audit write fail can't undo counting or lockout. No enumeration: unknown users save an event too, and the table has no foreign keys. A client abort can't cause it, because these writes ignore cancellation. |
| Saving the session after a right password reset the count | The reset. There is no session and no `SignedIn` event (one save); the client gets 500, or 409 on a conflict. | A verified password proves knowledge of it, so the reset gives an attacker nothing. ASP.NET Core Identity's SignInManager also resets before the sign-in completes. |
| Saving after a rehash | Nothing of the rehash: it rolls back with the session. The reset stays, if there was one. | The old hash still verifies the same password, and the next sign-in rehashes again. |

## Consequences
* `Migration03_IdentitySecurity` adds two tables and changes no existing table, index or temporal history.
* Role and access edit screens must send `version` and `revision`. Any change to a user can answer 409: reload and
  retry.
* Behind an unconfigured TLS-terminating proxy, sign-in is refused and every client shares one budget per operation.
* The POS must follow the refresh rules of decision 4.

## Known limitations
* A sign-in whose password was checked just before a password change commits can still create its session after
  the change has revoked the others (a window of about one hash). A later fix stores a password version on the
  session, which needs a migration.
* Lockout bounds sequential guessing. A parallel burst that loaded the account before the lockout gets one check per
  request, bounded only by the per-address limits.
* The Administrator guards serialize changes to Administrators through the role's version, but a change to a user
  who is being made an Administrator at that same moment isn't covered. The last company-wide Administrator could
  still remove themselves with deliberately parallel requests. Touching the role on every guarded change would fill
  its temporal history with empty versions.
* A deactivated user's previous refresh token is recorded as `SessionRevoked` (deactivation), not as
  `RefreshTokenReused`; the session ends either way.
* `identity.SecurityEvents` has no retention or cleanup yet.
* Event details record the id lists as sent (bounded by the request size; needs a manage permission).
* A huge `page` number answers 500 instead of 400 on every list endpoint (the shared paging rule).
* OpenAPI does not declare the global 401 and 403 of authenticated endpoints.

## Pending owner decisions
* Whether `identity.roles.manage` must be company-wide. Today a branch-scoped holder can edit roles that company-wide
  users hold.
* Whether a successful self-service password change should clear earlier failed attempts. Today it keeps them, as
  ASP.NET Core Identity does.
* The two **(new)** guards of decision 5.

## Still deferred
* Data Protection key encryption at rest.
* Padding refused logins to a fixed duration.
* A per-user session cap.
* Branch scope on warehouse creation (an Organization decision).
* Versions for user profile and role-assignment edits.
* Step-up re-authentication for user creation and password reset.
* MFA, SSO, and POS device registration.
