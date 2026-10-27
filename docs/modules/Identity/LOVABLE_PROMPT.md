# Lovable prompt — WarehouseERP Identity administration screens

Copy everything below the line into Lovable, in the same project as the Organization screens. It is built from the
backend's real OpenAPI contract (`docs/api/openapi-v1.json`) and an end-to-end run against the real API; don't invent
endpoints or fields.

---

Add **Identity administration** to the existing WarehouseERP app. It covers:
- users, their roles and their access (company-wide, branches, warehouses);
- roles and their permissions;
- the security log;
- each user's own password and sessions.

## Build on what exists (don't rebuild it)
The App Shell, sign-in, `/me` session, RTK Query base API, `can()` helper, ProblemDetails helper, translations,
`ServerTable`, `ConfirmDialog` and form fields already exist. Follow `AGENTS.md`:
- Inject the new endpoints into `src/shared/api/baseApi.ts`. Add tag types `User`, `Role`, `Session` and
  `SecurityEvent`.
- Add the Identity permission names to `P` in `src/shared/session/permissions.ts`. Check them with `can`/`canCompanyWide`.
- Put every new UI text in `src/shared/i18n/translations.ts`, and error texts under `err.<errorCode>`.
- In `AppShell.tsx`, add an **Administration** sidebar group after Organization with Users, Roles and Security log.
  Show each link only with its read permission.
- In the user menu, add **Change password** and **My sessions** above Sign out, for every signed-in user.
- Put pages under `src/routes/_authenticated/admin/...` and `src/routes/_authenticated/account/...`.

**Concurrency is different from Organization.** Identity edits don't use `rowVersion`, and every PUT/POST action
answers **204 with no body**:
- A role's details carry `version`; send it back in the role PUT.
- A user's details carry `access.revision`; send it back in the access PUT.
- After a successful save, refetch the record (invalidate its tag) to get the new `version`/`revision`.
- **409** `Common.ConcurrencyConflict`: show "This record was changed by someone else", reload it, and keep the
  user's edits visible to re-apply.

## Permissions and scope
`/me.permissions` may contain `"*"` (every permission). `isCompanyWide` comes from `/me`.

| Action | Needs |
|---|---|
| See users and user details | `identity.users.read` |
| Create user, edit profile, activate/deactivate, unlock, reset password, set roles | `identity.users.manage` |
| Set a user's access (company-wide, branches, warehouses) | `identity.access.manage` |
| See roles and the permission catalog | `identity.roles.read` |
| Create, edit, delete roles | `identity.roles.manage` **and** `isCompanyWide` |
| See and end another user's sessions | `identity.sessions.manage` |
| Security log | `identity.audit.read` |
| Change own password, see and end own sessions | signed in |

The server also refuses things the UI can't fully predict. Show its message (translated by `errorCode`):
- `Identity.Access.ExceedsOwnPermissions` (403): giving a role with permissions you don't hold, or managing a user
  who holds permissions you don't.
- `Identity.Access.ExceedsOwnScope` (403): giving company-wide access, or branches or warehouses outside your own
  access, or managing a user outside it. It is also the answer when someone without company-wide access edits roles.
- `Identity.User.LastAdministrator` (422): the last active company-wide Administrator must keep that role, that
  access and stay active.
- On your **own** user, hide deactivate, unlock and reset password. The server refuses them with
  `CannotDeactivateSelf`, `CannotUnlockSelf` and `CannotResetOwnPassword`; link to *Change password* instead.

## Shared API shapes
- Users list is paged: `{ items, page, pageSize, totalCount, totalPages }`, with query `page` (from 1), `pageSize`
  (1–100, default 20) and `search` (≤100). Search the server, debounced 300 ms.
- Roles, permissions and sessions are plain arrays (not paged).
- `201 Created` → `{ id }`. Every PUT and POST action → `204`, no body.
- **Errors** are ProblemDetails (same helper as Organization).
  - Password rule failures are a **400** `Validation.Failed`. Their `errors[].code` is one of `PasswordTooShort`,
    `PasswordRequiresDigit`, `PasswordRequiresLower`, `PasswordRequiresUpper`, `PasswordRequiresNonAlphanumeric` or
    `PasswordRequiresUniqueChars`.
  - The property is `password` when creating a user and `newPassword` when changing or resetting a password.
  - Translate those codes on the field.
- **Password policy:** `GET /api/identity/password-policy` → `{ requiredLength, requiredUniqueChars, requireDigit,
  requireLowercase, requireUppercase, requireNonAlphanumeric }` (today: 10 characters, a digit, a lowercase and an
  uppercase letter). Show it as a live checklist under every new-password field. The server stays the judge.
- **429** `Common.TooManyRequests` (sign-in, password change) comes with `Retry-After`: "Too many attempts, try again in
  a moment".

## 1. Users — `/admin/users`
- **List:** `GET /api/identity/users?page&pageSize&search`.
  - Items are `{ id, userName, displayName, email, isActive, isLockedOut }`.
  - `search` matches user name, display name or email.
  - Columns: user name (`dir="ltr"`), display name, email, status badge (Active / Inactive), plus a "Locked" badge.
  - The list is **not** filtered by your access: everyone with `identity.users.read` sees every user. Managing a user
    outside your access answers 403.
- **Create** (`/admin/users/new`): `POST /api/identity/users` with
  `{ userName, displayName, email, password, roleIds, isCompanyWide, branchIds, warehouseIds }`.
  - `userName`: required, ≤256, `A–Z a–z 0–9 . _ @ -`, unique ignoring case (409 `Identity.User.UserNameTaken` on
    the field). It can't be changed later.
  - `displayName`: required, ≤150. `email`: optional, ≤256, a valid address (send `null` when empty).
  - `password` with the policy checklist and a "show" toggle.
  - Roles: a multi-select fed by `GET /api/identity/roles`. Send `[]` for none, never `null`.
  - **Access** section, only with `identity.access.manage`:
    - a "Company-wide" switch; when on, hide and send empty branch and warehouse lists;
    - otherwise a branch multi-select (`GET /api/organization/branches?isActive=true&pageSize=100`) and a warehouse
      multi-select (`GET /api/organization/warehouses?isActive=true&pageSize=100`, grouped by branch).
    - Explain under it that a branch covers all of its warehouses, including ones added later, and that warehouses
      add single warehouses from other branches.
    - These lists are already limited to the signed-in user's own access.
  - Without `identity.access.manage`, send `isCompanyWide: false` and empty lists. Otherwise the server answers 403
    `Identity.Access.ManageRequired`.
  - Errors:
    - 409 `UserNameTaken`;
    - 422 `Identity.Role.UnknownRole`, `Identity.Access.UnknownBranch`, `Identity.Access.UnknownWarehouse`;
    - 403 `ManageRequired`, `ExceedsOwnScope`, `ExceedsOwnPermissions`.
  - After 201, open the new user's page.
- **Details** (`/admin/users/:id`): `GET /api/identity/users/{id}` →
  `{ id, userName, displayName, email, isActive, isLockedOut, roles: [{ id, name }],
     access: { isCompanyWide, branchIds, warehouseIds, revision } }`.
  - Header: display name, user name, status and locked badges, and the actions below.
  - Show access as names: resolve branch and warehouse ids from the Organization lists. Show "All branches" when
    company-wide.
  - **Profile:** `PUT /api/identity/users/{id}` with `{ displayName, email }` → 204. The user name is read-only.
  - **Roles:** `PUT /api/identity/users/{id}/roles` with `{ roleIds }` → 204. It replaces the whole set.
  - **Access** (`identity.access.manage`): `PUT /api/identity/users/{id}/access` with
    `{ isCompanyWide, branchIds, warehouseIds, revision }` → 204, using the same editor as create. 409 means reload.
  - **Activate / Deactivate** (`POST .../activate` or `/deactivate`, after a confirm dialog). Warn that deactivating
    signs the user out everywhere.
  - **Unlock** (`POST .../unlock`): only while `isLockedOut`.
  - **Reset password:** a dialog posting `POST /api/identity/users/{id}/password` with `{ newPassword }` → 204. Warn
    that it signs the user out everywhere.
  - **Sessions** tab (`identity.sessions.manage`): `GET /api/identity/users/{id}/sessions` (see section 4 for the
    shape), and an "End all sessions" button calling `POST /api/identity/users/{id}/sessions/revoke` → 204.

## 2. Roles — `/admin/roles`
- **List:** `GET /api/identity/roles` → `[{ id, name, description, isSystem, permissionCount }]`.
  - Columns: name, description, permission count, and a "System" badge.
  - A name filter on the client is fine; this list is small and not scoped.
- **Permission catalog:** `GET /api/identity/permissions` → `[{ name, module }]`.
  - Group it by `module`, with one checkbox per permission and a "select all" per module.
  - Labels come from the table at the end. An unknown name shows as-is.
- **Details / edit** (`/admin/roles/:id`, and `/admin/roles/new`): `GET /api/identity/roles/{id}` →
  `{ id, name, description, isSystem, grantsAllPermissions, permissions, version }`.
  - **Create:** `POST /api/identity/roles` with `{ name, description, permissions }` → 201 `{ id }`.
  - **Update:** `PUT /api/identity/roles/{id}` with `{ name, description, permissions, version }` → 204.
  - `name`: required, ≤100, unique ignoring case (409 `Identity.Role.NameTaken`). `description` ≤500.
    `permissions` is an array, possibly empty.
  - **Delete** (confirm): `DELETE /api/identity/roles/{id}` → 204. 409 `Identity.Role.InUse` means "Remove this role
    from its users first".
  - **System role** (`isSystem`, the Administrator, `grantsAllPermissions: true`):
    - read-only, with no edit or delete (the server answers 422 `Identity.Role.SystemRoleLocked`);
    - show "All permissions, including future ones".
  - Without `isCompanyWide`, show roles read-only; edits answer 403 `ExceedsOwnScope`.

## 3. Security log — `/admin/security-events`
- `GET /api/identity/security-events?page&pageSize&userId&type&fromUtc&toUtc`, newest first. Items are
  `{ id, occurredAtUtc, type, actorUserId, actorDisplayName, targetUserId, targetDisplayName, roleId, sessionId,
     ipAddress, details }`.
- Columns: time (browser local), event, by (`actorDisplayName`, or "—" for sign-ins), user (`targetDisplayName`),
  IP (`dir="ltr"`), and details.
- `details` is a small JSON string such as `{"channel":"web"}` or `{"reason":"Identity.Access.ExceedsOwnScope"}`.
  Show it as key: value. Translate a `reason` that is an error code.
- Filters:
  - event type: a select of the types in the table at the end;
  - user: a search select over users;
  - date range: send ISO UTC; `toUtc` must be after `fromUtc`.
- The log is read-only.

## 4. My account (user menu)
- **Change password** (`/account/password`): `POST /api/identity/me/password` with `{ currentPassword, newPassword }`.
  - Use a confirm field on the client only, and the policy checklist.
  - **204:** toast "Password changed. Your other sessions were signed out." This session stays signed in.
  - **422** `Identity.Password.Incorrect`: on the current-password field.
  - **422** `Identity.Password.LockedOut`: too many wrong attempts; try later or ask an administrator.
  - **400** password rules on `newPassword`, and **429**.
- **My sessions** (`/account/sessions`): `GET /api/identity/sessions` →
  `[{ id, clientType, deviceId, createdAtUtc, lastSeenAtUtc, expiresAtUtc, revokedAtUtc, revokedReason }]`, most
  recently used first.
  - `clientType` is `"Web"` or `"Api"` (POS).
  - Sessions that ended stay listed for 30 days. Grey them out and show `revokedReason`.
  - "End" on an active session: `POST /api/identity/sessions/{id}/revoke` → 204.
  - There is no "current session" flag. Ending the session you are using signs you out on the next request (401 →
    login).

## Permission labels
| Permission | Arabic | English |
|---|---|---|
| `identity.users.read` | عرض المستخدمين | View users |
| `identity.users.manage` | إدارة المستخدمين | Manage users |
| `identity.access.manage` | تحديد نطاق المستخدمين (الفروع والمخازن) | Set user access (branches and warehouses) |
| `identity.roles.read` | عرض الأدوار والصلاحيات | View roles and permissions |
| `identity.roles.manage` | إدارة الأدوار | Manage roles |
| `identity.sessions.manage` | إدارة جلسات المستخدمين | Manage user sessions |
| `identity.audit.read` | عرض سجل الأمان | View the security log |
| `organization.company.update` | إعداد وتعديل بيانات الشركة | Set up and edit the company |
| `organization.branches.read` | عرض الفروع | View branches |
| `organization.branches.create` | إضافة فروع | Create branches |
| `organization.branches.update` | تعديل وتفعيل وإيقاف الفروع | Edit, activate and deactivate branches |
| `organization.warehouses.read` | عرض المخازن ومواقعها | View warehouses and their locations |
| `organization.warehouses.create` | إضافة مخازن | Create warehouses |
| `organization.warehouses.update` | تعديل وتفعيل وإيقاف المخازن | Edit, activate and deactivate warehouses |
| `organization.locations.create` | إضافة مواقع تخزين | Add storage locations |
| `organization.locations.update` | تعديل وتفعيل وإيقاف المواقع | Edit, activate and deactivate locations |
| `catalog.products.read` | عرض الأصناف | View products |
| `catalog.products.create` | إضافة أصناف | Create products |
| `catalog.products.update` | تعديل الأصناف | Edit products |

Modules: `Identity` = المستخدمون والصلاحيات / Users and access; `Organization` = الهيكل التنظيمي / Organization;
`Catalog` = الأصناف / Catalog.

## Security event labels
| type | Arabic | English |
|---|---|---|
| `SignedIn` | تسجيل دخول | Signed in |
| `SignInFailed` | محاولة دخول فاشلة | Failed sign-in |
| `SignedOut` | تسجيل خروج | Signed out |
| `ApiTokenIssued` | دخول نقطة بيع (رمز API) | API token issued |
| `RefreshTokenReused` | إعادة استخدام رمز تجديد (أُنهيت الجلسة) | Refresh token reused (session ended) |
| `PasswordChanged` | تغيير كلمة المرور | Password changed |
| `PasswordChangeRejected` | رفض تغيير كلمة المرور | Password change rejected |
| `PasswordReset` | إعادة تعيين كلمة المرور | Password reset |
| `UserCreated` | إنشاء مستخدم | User created |
| `UserUpdated` | تعديل مستخدم | User updated |
| `UserActivated` | تفعيل مستخدم | User activated |
| `UserDeactivated` | إيقاف مستخدم | User deactivated |
| `UserUnlocked` | فك قفل مستخدم | User unlocked |
| `UserRolesChanged` | تغيير أدوار مستخدم | User roles changed |
| `UserAccessChanged` | تغيير نطاق مستخدم | User access changed |
| `RoleCreated` | إنشاء دور | Role created |
| `RoleUpdated` | تعديل دور | Role updated |
| `RoleDeleted` | حذف دور | Role deleted |
| `SessionRevoked` | إنهاء جلسة | Session ended |
| `UserSessionsRevoked` | إنهاء كل جلسات مستخدم | All of a user's sessions ended |
| `PrivilegeEscalationBlocked` | منع تجاوز صلاحيات | Privilege escalation blocked |
| `AdministratorBootstrapped` | إنشاء المدير الأول | First administrator created |
| `UserLockedOut` | قفل الحساب بعد محاولات خاطئة | Account locked out |

Session end reasons (`revokedReason`):

| Reason | Arabic | English |
|---|---|---|
| `Logout` | تسجيل خروج | Signed out |
| `RevokedByUser` | أنهاها المستخدم | Ended by the user |
| `RevokedByAdministrator` | أنهاها مدير | Ended by an administrator |
| `PasswordChanged` | تغيير كلمة المرور | Password changed |
| `PasswordReset` | إعادة تعيين كلمة المرور | Password reset |
| `UserDeactivated` | إيقاف المستخدم | User deactivated |
| `RefreshTokenReused` | إعادة استخدام رمز التجديد | Refresh token reused |

## Error codes to translate
| errorCode | Status | Arabic | English |
|---|---|---|---|
| `Identity.User.NotFound` | 404 | المستخدم غير موجود | The user does not exist |
| `Identity.User.UserNameTaken` | 409 | اسم المستخدم مستخدم بالفعل | Another user has this user name |
| `Identity.User.CannotDeactivateSelf` | 422 | لا يمكنك إيقاف حسابك | You can't deactivate your own account |
| `Identity.User.CannotResetOwnPassword` | 422 | غيّر كلمة مرورك من صفحة تغيير كلمة المرور | Change your own password from Change password |
| `Identity.User.CannotUnlockSelf` | 422 | يجب أن يفك مدير آخر القفل، أو انتظر انتهاءه | Another administrator must unlock you, or wait |
| `Identity.User.LastAdministrator` | 422 | يجب أن يبقى مدير نشط واحد على الأقل على مستوى الشركة | At least one active company-wide Administrator must remain |
| `Identity.Role.NotFound` | 404 | الدور غير موجود | The role does not exist |
| `Identity.Role.NameTaken` | 409 | يوجد دور آخر بنفس الاسم | Another role has this name |
| `Identity.Role.InUse` | 409 | الدور مُسند لمستخدمين، أزله منهم أولاً | The role is assigned to users; remove it from them first |
| `Identity.Role.UnknownRole` | 422 | دور أو أكثر غير موجود | One or more roles do not exist |
| `Identity.Role.UnknownPermission` | 422 | صلاحية غير معروفة | Unknown permission |
| `Identity.Role.SystemRoleLocked` | 422 | لا يمكن تعديل أو حذف دور النظام | A system role can't be changed or deleted |
| `Identity.Access.UnknownBranch` | 422 | فرع أو أكثر غير موجود | One or more branches do not exist |
| `Identity.Access.UnknownWarehouse` | 422 | مخزن أو أكثر غير موجود | One or more warehouses do not exist |
| `Identity.Access.ManageRequired` | 403 | منح الفروع أو المخازن أو نطاق الشركة يحتاج صلاحية تحديد النطاق | Giving access needs the "Set user access" permission |
| `Identity.Access.ExceedsOwnScope` | 403 | لا يمكنك منح نطاق خارج نطاقك أو إدارة مستخدم خارجه | You can't grant or manage access outside your own |
| `Identity.Access.ExceedsOwnPermissions` | 403 | لا يمكنك منح صلاحيات لا تملكها أو إدارة مستخدم يملكها | You can't grant permissions you don't hold, or manage a user who holds them |
| `Identity.Password.Incorrect` | 422 | كلمة المرور الحالية غير صحيحة | The current password is not correct |
| `Identity.Password.LockedOut` | 422 | تم قفل الحساب مؤقتًا بسبب محاولات خاطئة كثيرة | Too many wrong attempts; the account is locked for now |
| `Identity.Session.NotFound` | 404 | الجلسة غير موجودة | The session does not exist |
| `Identity.Session.AccessDenied` | 403 | يمكنك فقط عرض وإنهاء جلساتك | You can only see and end your own sessions |
| `Identity.Session.HttpsRequired` | 403 | هذا الإجراء يحتاج اتصالاً آمنًا (HTTPS) | This needs a secure (HTTPS) connection |
| `Common.TooManyRequests` | 429 | محاولات كثيرة، حاول بعد قليل | Too many attempts, try again shortly |
| `Common.BadRequest` | 400 | طلب غير صالح | Invalid request |

Password rule codes (`errors[].code`):

| Code | Arabic | English |
|---|---|---|
| `PasswordTooShort` | قصيرة جدًا | Too short |
| `PasswordRequiresDigit` | يجب أن تحتوي على رقم | Needs a digit |
| `PasswordRequiresLower` | يجب أن تحتوي على حرف صغير | Needs a lowercase letter |
| `PasswordRequiresUpper` | يجب أن تحتوي على حرف كبير | Needs an uppercase letter |
| `PasswordRequiresNonAlphanumeric` | يجب أن تحتوي على رمز | Needs a symbol |
| `PasswordRequiresUniqueChars` | تحتاج حروفًا مختلفة أكثر | Needs more different characters |

## Backend gaps (don't fake them)
- The permission catalog has no display names. Use the table above.
- `/me` has no branch ids, and session lists have no "current session" flag.
- The users list isn't filtered by the signed-in user's access.
- Users can't be deleted (deactivate them) and user names can't change.
