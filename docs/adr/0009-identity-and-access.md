# ADR 0009 — Identity & Access: server-side sessions, permission roles, user scope

Status: accepted (2026-10-26). Supersedes the "JWT bearer only with a trusted issuer" and `NoIdentityProvider` parts
of ADR 0005; the rest of ADR 0005 (`ICurrentUser`, permission policies, scope in the handler) stands.
Design: [`docs/design/identity-access-module.md`](../design/identity-access-module.md).

## Context
The API needs its own users, sign-in for a React client and for the POS, permission-based authorization with branch
and warehouse scope, and immediate revocation. Other modules must keep depending only on `ICurrentUser` and
Contracts. Only an additive migration is allowed; existing tables must not change.

## Decision (the five approved points)
1. **Authentication = server-side sessions** (`identity.UserSessions`, SHA-256 token hashes only). The browser holds
   the token in an HttpOnly, Secure, SameSite=Strict `__Host-erp` cookie, plus anti-forgery (`XSRF-TOKEN` cookie →
   `X-XSRF-TOKEN` header on every unsafe cookie request). The POS and API tools get a 15-minute opaque bearer access
   token and a refresh token that rotates on every use; presenting a used refresh token ends the session. No external
   JWT issuer: one user store.
2. **Authorization = global roles + per-user scope.** A role is a set of permissions (`identity.RoleClaims`, type
   `permission`). Scope (`identity.UserAccess`) is company-wide, or branches (each covers its warehouses, including
   ones created later) plus single warehouses. The system role **Administrator** holds every permission by definition.
3. **Ownership.** Identity owns users, roles, scope and sessions; Organization owns branches and warehouses, read
   through `IBranchDirectory`/`IWarehouseDirectory`. The foreign keys from grants to `organization.*` live in
   `ErpDbContext` (ADR 0001).
4. **One context.** `ErpDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`, tables in the
   `identity` schema. Temporal history on roles, role claims, user roles, scope and grants; not on users (old password
   hashes) or sessions (operational). ASP.NET Core Identity's store saves nothing on its own (`AutoSaveChanges` off):
   every use case commits once through `IIdentityPersistence`.
5. **First administrator** from `Identity:Bootstrap` (environment or secret store) while no user exists; ignored after.

Per request, the session handler looks the token up, refuses a revoked, expired or idle session or an inactive user,
and resolves permissions and effective warehouses fresh (a few indexed reads), so role, permission and scope changes
apply on the next request without signing in again. `ICurrentUser` (`HttpCurrentUser`) moved from the host into the
Identity module; permission policies are built from the catalog every module registers (`AddPermissions`).

## Added during implementation (approved by the product owner, 2026-10-26)
* **No privilege escalation through Identity.** Nobody grants a role holding permissions they don't hold, edits a role
  to include them, or manages (edits, resets, re-roles, re-scopes, deactivates, unlocks) a user who holds more than
  they do; scope is granted only from within the caller's own scope (company-wide only by a company-wide caller, a
  branch only by a holder of that branch). Otherwise `identity.users.manage` alone would be enough to become
  Administrator. Codes: `Identity.Access.ExceedsOwnPermissions`, `Identity.Access.ExceedsOwnScope` (403). Revoking
  sessions is exempt: it never grants anything.
* Creating a user **with scope** also needs `identity.access.manage` (`Identity.Access.ManageRequired`), as setting
  the scope later would.
* **HTTPS required** for login, token issue/refresh and password change (`Identity.Session.HttpsRequired`, 403).
  Behind a TLS-terminating proxy, the host must apply forwarded headers.
* Every refused login takes about one password-hash check (unknown and locked-out users verify against a dummy hash),
  and a conflict saving a failed-attempt count still answers `Identity.Login.Failed`, so neither timing nor a 409
  reveals which user names exist. A wrong current password on password change counts toward lockout.
* Data Protection keys (used only for anti-forgery tokens; session tokens are random values, not protected payloads)
  are stored in `identity.DataProtectionKeys` so restarts and several instances share them.

## Consequences
* One extra table lookup and a few small queries per authenticated request; measure before adding a cache (a cache
  would delay revocation and permission changes).
* Development runs the API over HTTPS (`dotnet dev-certs https --trust`); the Vite proxy targets the https URL.
* Tests use the real session scheme for Identity and the header-based test scheme for other modules.

## Deferred
Per-IP rate limiting of sign-in endpoints (lockout is per account only); a cleanup job for expired sessions;
encryption at rest of the Data Protection keys (certificate or key vault); forwarded-headers configuration (depends
on the deployment); optimistic concurrency for role edits from the client (last write wins today; user scope edits
are protected by `UserAccess.Revision`); MFA; external SSO; POS device registration and offline grants (design §7);
an operational AuditLog for identity events (today: Serilog events plus temporal history).
