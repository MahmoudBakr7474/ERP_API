# Identity — frontend handoff (administration screens)

Verified 2026-10-27 against the real API. The run used a throwaway LocalDB database, real cookie sessions and
anti-forgery, and the throwaway database was then dropped. The examples show the real shapes, status codes and error
codes; ids and versions are placeholders.

The Lovable prompt is [`LOVABLE_PROMPT.md`](LOVABLE_PROMPT.md). Decisions and their reasons are in
[ADR 0012](../../adr/0012-identity-hardening.md) and [ADR 0009](../../adr/0009-identity-and-access.md).

## Where it goes
- These screens extend the existing ERP frontend: Lovable project `mahmoudtaha311/screenshot-match`, cloned locally to
  `D:\ERP System\warehouse-erp-web`.
- They reuse the App Shell, sign-in, `/me` session, RTK Query base API and `can()`. Nothing about sign-in changes.
- Running the backend, the Vite proxy and the cookie/XSRF rules are the same as in
  [Organization's handoff](../Organization/FRONTEND_HANDOFF.md).
  - Creating a user and resetting a password need HTTPS (`Identity.Session.HttpsRequired`).
  - The Vite proxy reaches the API over https, so this works in development.
- OpenAPI tags: `Users`, `Roles`, `Sessions`, `Security events`, and `Identity` for the account endpoints.

## Endpoints
| Method | Path | Permission (+ scope) | Success | Errors |
|---|---|---|---|---|
| GET | `/api/identity/password-policy` | signed in | 200 `PasswordPolicyResponse` | |
| POST | `/api/identity/me/password` | signed in | 204 | 400, 422 Incorrect / LockedOut, 429 |
| GET | `/api/identity/sessions` | signed in (own) | 200 `SessionResponse[]` | |
| POST | `/api/identity/sessions/{id}/revoke` | signed in (own) | 204 | 404 |
| GET | `/api/identity/users?page&pageSize&search` | `identity.users.read` | 200 paged | 400 |
| GET | `/api/identity/users/{id}` | `identity.users.read` | 200 `UserDetailsResponse` | 404 |
| POST | `/api/identity/users` | `identity.users.manage` (+ `identity.access.manage` to give access) | 201 `{id}` | 400, 403, 409, 422 |
| PUT | `/api/identity/users/{id}` | `identity.users.manage` + target in scope | 204 | 400, 403, 404, 409 |
| POST | `/api/identity/users/{id}/activate` | `identity.users.manage` + target in scope | 204 | 403, 404, 409 |
| POST | `/api/identity/users/{id}/deactivate` | `identity.users.manage` + target in scope | 204 | 403, 404, 409, 422 |
| POST | `/api/identity/users/{id}/unlock` | `identity.users.manage` + target in scope | 204 | 403, 404, 409, 422 |
| POST | `/api/identity/users/{id}/password` | `identity.users.manage` + target in scope | 204 | 400, 403, 404, 409, 422 |
| PUT | `/api/identity/users/{id}/roles` | `identity.users.manage` + target in scope | 204 | 400, 403, 404, 409, 422 |
| PUT | `/api/identity/users/{id}/access` | `identity.access.manage` + target and new access in scope | 204 | 400, 403, 404, 409, 422 |
| GET | `/api/identity/users/{id}/sessions` | `identity.sessions.manage` | 200 `SessionResponse[]` | |
| POST | `/api/identity/users/{id}/sessions/revoke` | `identity.sessions.manage` | 204 | 404 |
| GET | `/api/identity/roles` | `identity.roles.read` | 200 `RoleSummaryResponse[]` | |
| GET | `/api/identity/roles/{id}` | `identity.roles.read` | 200 `RoleDetailsResponse` | 404 |
| GET | `/api/identity/permissions` | `identity.roles.read` | 200 `PermissionDefinition[]` | |
| POST | `/api/identity/roles` | `identity.roles.manage` + company-wide | 201 `{id}` | 400, 403, 409, 422 |
| PUT | `/api/identity/roles/{id}` | `identity.roles.manage` + company-wide | 204 | 400, 403, 404, 409, 422 |
| DELETE | `/api/identity/roles/{id}` | `identity.roles.manage` + company-wide | 204 | 403, 404, 409 InUse, 422 |
| GET | `/api/identity/security-events?page&pageSize&userId&type&fromUtc&toUtc` | `identity.audit.read` | 200 paged | 400 |

**Rules behind the 403/422 answers** (ADR 0012, decision 5):
- "Target in scope" means the user being managed has access within the caller's own.
- Giving a role also needs every permission in it (`ExceedsOwnPermissions`). The Administrator role, and managing
  its holders, need `"*"`.
- Nobody can deactivate, unlock or reset the password of their own account through administration.
- The last active company-wide Administrator keeps that role, that access and stays active.

**Sessions end automatically** after a password change (all sessions but the current one), a password reset, or a
deactivation (all sessions).

## Request / response examples
```http
GET /api/identity/password-policy
→ 200 { "requiredLength": 10, "requiredUniqueChars": 1, "requireDigit": true, "requireLowercase": true,
        "requireUppercase": true, "requireNonAlphanumeric": false }

GET /api/identity/permissions
→ 200 [ { "name": "catalog.products.create", "module": "Catalog" }, …,
        { "name": "identity.access.manage", "module": "Identity" }, …,
        { "name": "organization.warehouses.update", "module": "Organization" } ]     (19 permissions today)

GET /api/identity/roles
→ 200 [ { "id": "3b0c6d0e-5f7a-4c1e-9a52-7d4f1e2b8c61", "name": "Administrator",
          "description": "Every permission, including user and role management.", "isSystem": true, "permissionCount": 19 },
        { "id": "…", "name": "Branch clerk", "description": "Reads branches and warehouses, edits locations",
          "isSystem": false, "permissionCount": 3 } ]

POST /api/identity/roles
{ "name": "Branch clerk", "description": "Reads branches and warehouses, edits locations",
  "permissions": ["organization.branches.read", "organization.warehouses.read", "organization.locations.update"] }
→ 201 { "id": "…" }                    (same name in other case → 409 Identity.Role.NameTaken;
                                         "no.such.permission" → 422 Identity.Role.UnknownPermission)

GET /api/identity/roles/{id}
→ 200 { "id": "…", "name": "Branch clerk", "description": "…", "isSystem": false, "grantsAllPermissions": false,
        "permissions": ["organization.branches.read", "organization.locations.update", "organization.warehouses.read"],
        "version": "7f37068e-7906-4cd7-b501-3e0676b982f6" }

PUT /api/identity/roles/{id}   { "name": "Branch clerk", "description": "Branch staff", "permissions": [ … ], "version": "7f37…" }
→ 204                           (same version again → 409 Common.ConcurrencyConflict)

PUT /api/identity/roles/3b0c6d0e-…   (Administrator)   → 422 Identity.Role.SystemRoleLocked
DELETE /api/identity/roles/{id}      (role in use)     → 409 Identity.Role.InUse

POST /api/identity/users
{ "userName": "clerk.cai", "displayName": "Clerk Cairo", "email": "clerk@example.com", "password": "…",
  "roleIds": ["…"], "isCompanyWide": false, "branchIds": ["…CAI…"], "warehouseIds": [] }
→ 201 { "id": "…" }

POST /api/identity/users   { …, "password": "short", … }
→ 400 { "errorCode": "Validation.Failed", "errors": [
          { "property": "password", "code": "PasswordTooShort", "message": "…" },
          { "property": "password", "code": "PasswordRequiresDigit", "message": "…" },
          { "property": "password", "code": "PasswordRequiresUpper", "message": "…" } ] }

POST /api/identity/users   { "userName": "bad name", "displayName": "", "email": "not-an-email", … }
→ 400 errors: userName RegularExpressionValidator, displayName NotEmptyValidator, email EmailValidator

GET /api/identity/users?search=cai
→ 200 { "items": [ { "id": "…", "userName": "clerk.cai", "displayName": "Clerk Cairo", "email": "clerk@example.com",
                     "isActive": true, "isLockedOut": false }, … ], "page": 1, "pageSize": 20, "totalCount": 2, "totalPages": 1 }

GET /api/identity/users/{id}
→ 200 { "id": "…", "userName": "clerk.cai", "displayName": "Clerk Cairo", "email": "clerk@example.com",
        "isActive": true, "isLockedOut": false, "roles": [ { "id": "…", "name": "Branch clerk" } ],
        "access": { "isCompanyWide": false, "branchIds": ["…"], "warehouseIds": [], "revision": 0 } }

PUT /api/identity/users/{id}/access   { "isCompanyWide": false, "branchIds": ["…CAI…"], "warehouseIds": ["…WH-ALX…"], "revision": 0 }
→ 204                                  (revision 0 again → 409 Common.ConcurrencyConflict)

GET /api/identity/sessions
→ 200 [ { "id": "…", "clientType": "Web", "deviceId": null, "createdAtUtc": "2026-10-27T15:14:36.033Z",
          "lastSeenAtUtc": "2026-10-27T15:14:36.033Z", "expiresAtUtc": "2026-10-28T03:14:36.033Z",
          "revokedAtUtc": null, "revokedReason": null } ]

GET /api/identity/security-events?page=1&pageSize=3
→ 200 { "items": [
          { "id": 41, "occurredAtUtc": "2026-10-27T15:14:44.709Z", "type": "SignedIn", "actorUserId": null,
            "actorDisplayName": null, "targetUserId": "…", "targetDisplayName": "Administrator", "roleId": null,
            "sessionId": "…", "ipAddress": "::1", "details": "{\"channel\":\"web\"}" },
          { "id": 40, "type": "PrivilegeEscalationBlocked", "actorDisplayName": "Manager Cairo",
            "details": "{\"reason\":\"Identity.Access.ExceedsOwnScope\"}", … } ], … }
```

## Integration test steps (manual, against the running API)
Prepare first: the company is set up, there are branches CAI and ALX with a warehouse each, and you are signed in as
the bootstrap administrator.

1. **Roles:**
   - Open Roles: the Administrator role is read-only, with "All permissions".
   - Create "Branch clerk" with branch read, warehouse read and location update. The same name again → 409.
   - Edit it twice from the same page load: the second save → 409, and the page reloads.
2. **Create a user:**
   - Create `clerk.cai` with role Branch clerk and branch CAI.
   - A weak password shows the policy errors on the field. A duplicate user name → 409 on the field.
3. **User details:**
   - Change the display name.
   - Add warehouse WH-ALX to the access: it shows as a name, not an id.
   - Remove a role and add it back.
   - Deleting Branch clerk → 409 InUse.
4. **Your own row:** on your own user, deactivate, unlock and reset password are hidden. Removing your own
   Administrator role → 422 LastAdministrator.
5. **Sign in as `clerk.cai`:**
   - Only the Organization screens the role allows appear, and no Administration group.
   - Change password: a wrong current password → 422 on the field; the right one → 204, and you stay signed in.
   - My sessions lists this browser.
6. **As administrator:**
   - The clerk's Sessions tab → End all sessions: the clerk's next request goes to sign-in.
   - Reset the clerk's password.
   - Five wrong sign-ins lock the clerk: the "Locked" badge appears, and Unlock clears it.
   - Deactivate: the clerk can't sign in. Activate: the clerk can again.
7. **Branch manager:**
   - Create role "Branch manager" with users read/manage, access manage, roles read/manage, branch read and
     warehouse read. Create `mgr.cai` with it and branch CAI.
   - Sign in as mgr.cai.
   - Creating a user in ALX, or a company-wide one → 403 ExceedsOwnScope.
   - Giving Branch clerk (it holds location update, which the manager lacks) → 403 ExceedsOwnPermissions.
   - A role holding only branch/warehouse read can be given to a new CAI user → 201.
   - Creating a role → 403 (the manager isn't company-wide).
   - Security log → 403.
8. **As administrator, Security log:**
   - Newest first. The blocked attempts show as "Privilege escalation blocked" with the reason.
   - Filtering by type "User created" works.

Every API call in these steps was checked in the verification run, with the expected status and error code. The API
log had no 500s and no passwords.
