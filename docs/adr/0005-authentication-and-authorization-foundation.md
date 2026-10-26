# ADR 0005 — Authentication and authorization foundation

Status: accepted (2026-10-26)

## Context
There is no identity module or user table yet, and none may be added in this refactoring. The API must still be
protected by permissions with warehouse (later branch) scope, and must not ship a test token issuer.

## Decision
* **Identity-provider agnostic.** Handlers see `ICurrentUser` (user id, permissions, warehouse scope); the host
  implements it from claims (`HttpCurrentUser`). Claim names: `sub`, `permission` (one per permission),
  `warehouse` (one per warehouse id, or `*` for all).
* **Permissions as policies.** Policy name = permission name, declared in each module's Contracts
  (`CatalogPermissions.ProductsRead`); the host registers a policy per permission. The fallback policy requires
  an authenticated user, so a new endpoint is protected unless it opts out (`AllowAnonymous`: health, OpenAPI).
* **Scope in the handler.** Resource-level checks (warehouse scope) are use-case logic: `GetWarehouseHandler`
  checks scope before existence (403 either way, so existence does not leak), `ListWarehousesHandler` filters.
* **JWT bearer only with a trusted issuer.** When `Authentication:Jwt` (Authority, Audience) is configured, the
  API validates tokens from that issuer (keys from its OpenID metadata, https required, options validated at
  startup). Without it, a `NoIdentityProvider` scheme authenticates nobody: protected endpoints answer 401.
* Tests use a test authentication scheme (headers → claims) registered only by the test host.

## Deferred
An Identity module (users, roles, permission assignment, `UserWarehouseAccess`, login/token issuing or an
external IdP) before production use; branch scope; OpenAPI security scheme.

## Consequences
Development without an identity provider can only call anonymous endpoints. Swapping the provider changes host
configuration only; handlers and policies stay.
