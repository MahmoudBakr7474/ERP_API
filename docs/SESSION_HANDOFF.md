# Session handoff — WarehouseERP

Last verified: 2026-10-27. Read this first; no need to re-analyse the repository.

## Branch and commits
- Branch: `feature/identity-completion`, not merged. `main` = `67a7473` (merge of feature/identity-access).
- On top of `1146099` (ADR 0011 standard), two commits hold the Identity completion:
  1. `5498eb5` — Identity implementation and tests (`src/**`, `tests/**`, `docs/api/openapi-v1.json`), 106 files.
  2. The documentation and ADR commit on top of it (README, ARCHITECTURE, CLAUDE.md, ADR 0009, ADR 0012, this file);
     a file can't name its own commit, so take its hash from `git log -1`.
- `src/WarehouseERP.Api/Properties/launchSettings.json` stays untracked and is never committed. It is the owner's
  local file (ports 49571/49572).
- Merging into `main` needs the owner's approval.

## What the Identity completion contains
Everything in `docs/adr/0012-identity-hardening.md` (accepted 2026-10-27):
- security audit trail (`GET /api/identity/security-events`, permission `identity.audit.read`);
- per-operation credential limits per client address (429), sized for branches behind one public address;
- atomic lockout counting that a stale user save can't overwrite (`ConcurrencyStamp`);
- strict refresh-token rotation with reuse detection at any depth;
- scope and role guards on user management, including the last company-wide Administrator;
- role `version` and access `revision` (409 on stale edits);
- expired-session cleanup;
- HTTPS for credentials, tokens ignored over plain HTTP, forwarded headers from trusted proxies only;
- UTC times;
- a committed OpenAPI contract with declared statuses and typed ProblemDetails extensions.

`Migration03_IdentitySecurity` only creates `identity.SecurityEvents` and `identity.RetiredRefreshTokens`.

## Owner decisions applied (approved 2026-10-27)
1. Strict refresh-token rotation and reuse detection: a second use of any refresh token ends the session.
2. Self-service password change needs the current password; no self reset through administration.
3. HTTPS enforcement; forwarded headers only from the `ReverseProxy` proxies, and none at all when it is off.
4. Strict JSON numbers for all request bodies; verified OpenAPI contract (body-bound commands are `...Request`).
5. Per-operation rate limits (`Identity:SignInRateLimit:<operation>`), defaults Login 60, IssueApiToken 60,
   RefreshApiToken 300, ChangeMyPassword 30 per minute per client address; an IPv6 client is its /64.
6. The ConcurrencyStamp correction.

## Fixed by the pre-commit review
Two multi-agent reviews ran (read-only finders, each finding checked by a skeptic). Fixed, each with a test:
- Administrative password reset, and unlock of a user with failed attempts, always answered 409. This was already
  broken on `main`: each UserManager step re-attached the tracked user.
- The last company-wide Administrator could narrow their own scope or lose the role, and nobody could restore
  company-wide administration. **New rule, owner to confirm.**
- An administrator could unlock their own account, undoing the lockout that protects password change.
  New code `Identity.User.CannotUnlockSelf` (422). **New rule, owner to confirm.**
- The Administrator role's version was read after the count, so two parallel removals could both commit.
- A refused login of an inactive user also saved a rehash and `UpdatedAtUtc`, and could lose its event.
- An aborted request could drop the events of an attempt that was already counted.
- A lockout started from `POST /me/password` recorded no `UserLockedOut` event.
- A refresh that ended a deactivated user's session recorded no event. An old token replayed on an ended session
  logged a revocation that didn't happen.
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` made the host trust every peer's forwarded headers.
- OpenAPI omitted reachable statuses on Identity endpoints and the ProblemDetails extensions. The snapshot test
  passed while `ERP_UPDATE_OPENAPI_SNAPSHOT=1` was set.

## Login transaction boundaries (verified on SQL Server)
No transaction spans a login: the count, a lockout and a reset commit first; events, session and rehash follow in
one save. A failed events save keeps the count (500, fail-secure). A failed session save keeps the reset, with no
session and no `SignedIn` event. A failed save after a rehash keeps the old hash. Details: ADR 0012, *Login
transaction boundaries*; tests `LoginSaveFailureTests`, `LockoutConcurrencyTests`.

## Verification (2026-10-27, LocalDB, on the committed tree)
- `dotnet build WarehouseERP.slnx -m:1`: 0 warnings, 0 errors.
- `dotnet test tests/<Project> --no-build`, each project separately:

| Project | Result |
|---|---|
| UnitTests | 117/117 passed |
| ArchitectureTests | 54/54 passed |
| IntegrationTests | 126/126 passed |

- The tests with parallel requests (lockout counting, racing refreshes), the rate-limit, transport and save-failure
  tests passed 5 runs in a row.
- Each new fix's test failed before the fix: admin reset and unlock (409 instead of 204), the rehash login (409),
  the same-request save, the forwarded-headers switch (204 instead of 403 with the fix reverted), the rate limits.
- `dotnet ef migrations has-pending-model-changes`: none. Migration01 and Migration02 are identical to HEAD; the
  snapshot diff is +82/-0.
- Migration03 was applied, reverted and re-applied on a throwaway LocalDB database, which was then dropped. Its SQL
  only creates the two new tables and their indexes.
- No secrets, stray files or whitespace errors in the diff.

## Known limitations
- A sign-in checked just before a password change commits can keep its session (fix needs a session password
  version, so a migration).
- Lockout bounds sequential guessing; a parallel burst is bounded only by the per-address limits.
- The last company-wide Administrator could still remove themselves with deliberately parallel requests that also
  promote and narrow another user (the guards don't touch the role for non-Administrators, to keep its temporal
  history clean).
- A deactivated user's previous refresh token is recorded as `SessionRevoked`, not `RefreshTokenReused`; the
  session ends either way.
- `identity.SecurityEvents` has no retention or cleanup.
- Event details record id lists as sent.
- A huge `page` number answers 500 instead of 400 on every list endpoint (shared paging rule).
- OpenAPI does not declare the global 401/403 of authenticated endpoints.
- Still deferred (ADR 0012): Data Protection key encryption, padding refused logins, a per-user session cap, branch
  scope on warehouse creation, versions for profile and role-assignment edits, step-up re-authentication, MFA, SSO,
  POS device registration.

## Pending owner decisions
1. Confirm the two new guards: the last company-wide Administrator, and `Identity.User.CannotUnlockSelf`.
2. Whether `identity.roles.manage` must be company-wide (a branch-scoped holder can edit roles that company-wide
   users hold).
3. Whether a successful self-service password change clears earlier failed attempts (today it keeps them).

## Pending work
- Frontend handoff: `docs/modules/Identity/LOVABLE_PROMPT.md` and `FRONTEND_HANDOFF.md` don't exist yet. The
  contract facts below and `docs/api/openapi-v1.json` are the source for them.
- Merge `feature/identity-completion` into `main` after the owner's review.
- Remove the old review worktree when no longer needed: `git worktree remove "../review-1146099"`.

## Contract facts (verified from `docs/api/openapi-v1.json` and code)
- **Permissions**
  - Identity: `identity.users.read`, `identity.users.manage`, `identity.access.manage`, `identity.roles.read`,
    `identity.roles.manage`, `identity.sessions.manage`, `identity.audit.read`.
  - Organization: `organization.warehouses.read`, `organization.warehouses.create`.
  - Catalog: `catalog.products.read`, `catalog.products.create`, `catalog.products.update`.
- **Endpoint permissions**
  - users read → `users.read`; users write → `users.manage`, except `PUT /users/{id}/access`, which needs
    `access.manage`;
  - `/users/{id}/sessions` and `/users/{id}/sessions/revoke` need `sessions.manage`;
  - `GET /roles`, `GET /roles/{id}` and `GET /permissions` need `roles.read`; role writes need `roles.manage`;
  - `/security-events` needs `audit.read`;
  - `/me`, `/me/password`, `/password-policy`, `/sessions` and `/logout` need a signed-in user;
  - `/login`, `/tokens` and `/tokens/refresh` are anonymous.
- **Error body**: ProblemDetails with `errorCode` and `traceId` always, `correlationId` when present, and `errors[]`
  (`property`, `code`, `message`) on validation failures. The OpenAPI schema declares them.
- **Error codes**
  - 400: `Validation.Failed`, `Common.BadRequest`.
  - 401: `Common.Unauthorized`, `Identity.Login.Failed`, `Identity.Session.Revoked`,
    `Identity.Session.RefreshTokenReused`.
  - 403: `Common.Forbidden`, `Identity.Access.ExceedsOwnPermissions`, `Identity.Access.ExceedsOwnScope`,
    `Identity.Access.ManageRequired`, `Identity.Session.AccessDenied`, `Identity.Session.CsrfTokenInvalid`,
    `Identity.Session.HttpsRequired`.
  - 404: `Identity.User.NotFound`, `Identity.Role.NotFound`, `Identity.Session.NotFound`.
  - 409: `Common.ConcurrencyConflict` (stale `version`/`revision`, or a user changed since it was loaded: reload and
    retry), `Identity.User.UserNameTaken`, `Identity.Role.NameTaken`, `Identity.Role.InUse`.
  - 422: `Identity.Password.Incorrect`, `Identity.Password.LockedOut`, `Identity.User.CannotDeactivateSelf`,
    `Identity.User.CannotResetOwnPassword`, `Identity.User.CannotUnlockSelf`, `Identity.User.LastAdministrator`,
    `Identity.Role.SystemRoleLocked`, `Identity.Role.UnknownPermission`, `Identity.Role.UnknownRole`,
    `Identity.Access.UnknownBranch`, `Identity.Access.UnknownWarehouse`.
  - 429: `Common.TooManyRequests`, with a `Retry-After` header.
  - 500: `Common.Unexpected`.
- **Browser session**
  - The HttpOnly cookie `__Host-erp` holds the session; `XSRF-TOKEN` is a readable cookie whose value goes back as
    the `X-XSRF-TOKEN` header on POST, PUT and DELETE; `GET /api/identity/me` renews it.
  - There is no CORS, so the frontend must be same-origin (a Vite proxy for `/api`).
  - The dev API runs at `https://localhost:49571`; run `dotnet dev-certs https --trust` first.
- **POS tokens**: one refresh in flight for all callers; refresh shortly before `accessTokenExpiresAtUtc`, not on
  every 401; never resend a refresh automatically; `Identity.Session.RefreshTokenReused` or
  `Identity.Session.Revoked` means signed out.
- **Backend gaps for the frontend**
  - There is no branch list endpoint.
  - Access returns ids only (warehouse names come from `GET /api/organization/warehouses`).
  - Permissions have no display names.

## Working rules
- Reply in Arabic.
- LocalDB only, never `Server=.`.
- One module at a time: Identity is finished; do not start another module without the owner.
- No changes to Migration 01, existing schemas or data.
- No force push, reset or merge to `main` without approval.
- Build with `-m:1` and run test projects separately. If a build runs out of memory, run
  `dotnet build-server shutdown`. If every integration test fails instantly, run `sqllocaldb start MSSQLLocalDB`.
- Stage commits with explicit paths and keep `launchSettings.json` out
  (`git add -- src tests docs/api/openapi-v1.json ':(exclude)src/WarehouseERP.Api/Properties/launchSettings.json'`).

## Exact next action
Get the owner's answers to the pending decisions above, then the frontend handoff files, then the merge to `main`.
