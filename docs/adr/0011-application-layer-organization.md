# ADR 0011 — Application layer organization: vertical slices by business area, contracts on reuse

Status: accepted (2026-10-26). Permanent project standard for every module, current and future. Refines ADR 0010
(business area + use case folders) and keeps it; supersedes the "Dto" naming tried briefly before this ADR.
Summary for day-to-day work: `CLAUDE.md` → *Application Layer Organization & Vertical Slice Conventions*; full table:
`docs/ARCHITECTURE.md` → *Application layer organization*.

## Context
Modules keep growing (Identity has 24 use cases; Inventory, Purchasing, Sales, POS and Accounting will add many more).
Before this decision the Application layers mixed several concerns in single files: a `Ports.cs` with 16 interfaces,
records and enums, module-wide error classes (`IdentityApplicationErrors` with 17 errors), response records declared
beside commands, and one interface (`IUserAccounts`) serving login, profile management and role assignment. HTTP
request records duplicated commands field for field.

## Decision
1. **Features by business area, then use case.** `Application/Features/<Area>/<UseCase>/` holds the use case's
   Command or Query, Handler, Validator and the models only it uses (`<UseCase>Response` or a meaningful name). Use
   cases don't reference each other; areas don't reference each other; area-level helpers serve one area only.
2. **Contracts on genuine reuse only.** A model used by one use case stays in its folder. It moves to
   `Application/Contracts/<Area>/` when several use cases use it or a port returns it (a port can't depend on one
   feature's folder). Names end in `Request`/`Response` by meaning; no `Contracts/DTOs`, no mandatory
   `Requests`/`Responses` subfolders, no automatic `Dto` suffix. A name alone does not make a type HTTP-specific.
3. **Application contracts are not module contracts.** `<Module>.Application/Contracts` is internal to the module.
   Other modules only use the `<Module>.Contracts` assembly (directories, permissions, enums); they never reference
   another module's Application project (enforced by the existing boundary tests).
4. **No duplicate models.** Commands and queries are the use-case contract. A Presentation request record exists only
   when HTTP genuinely differs (an id from the route, a server-set field such as the caller's session, an optional
   list the endpoint turns into an empty one); it lives in `Presentation/Contracts/<Area>/`. Otherwise the endpoint
   binds the command. `CreatedResponse`/`UpdatedResponse`, shared by every module, live in
   BuildingBlocks.Infrastructure.
5. **Interfaces by responsibility.** `Application/Abstractions/Interfaces/<Group>/`, one interface (or port model) per
   file. Segregate where consumers need only part: `IUserAccounts` became `IUserAccounts` (find, create, profile,
   activation), `IUserCredentials` (password checks, changes, resets, unlock) and `IUserRoleAssignments` (who holds
   which role). No splitting into tiny interfaces without a consumer that benefits.
6. **Errors by business area.** `Application/Errors/<Area>Errors.cs` (e.g. Identity: Authentication, User, Role,
   Permission, UserAccess, Session). Domain invariant errors stay in the Domain as `<Entity>DomainErrors`, so the
   Domain never depends on the Application and the two sets never share a name. Error codes are unchanged.
7. **Files.** One top-level type per file, file named after it, namespace = folder (IDE0130 is a build error in
   `src/Modules`). EF Core configurations sit in `Infrastructure/Persistence/Configurations/`.

## Why
* **Business area grouping** keeps everything about one capability in one place: a developer looking for "deactivate
  a user" opens `Features/Users/SetUserActive/` and finds every type it needs, with nothing to chase in global
  Commands/Handlers/Validators folders.
* **Feature-specific contracts stay in the feature** because moving them for symmetry scatters a use case across the
  tree and creates a shared folder that slowly fills with single-use types nobody dares to change.
* **Reusable contracts move to `Contracts/<Area>`** when reuse is real; a port returning a model is reuse by
  definition, and the port must not depend on a feature.
* **No mandatory Requests/Responses levels**: one level per area is enough to find a contract; extra levels add
  navigation without adding meaning.

## Alternatives considered
* *Global Commands / Queries / Handlers / Validators folders*: groups by technical kind, splits every use case over
  four places. Rejected.
* *Everything shared in `Contracts/DTOs` with a `Dto` suffix*: tried first in this refactoring; it moved single-use
  models away from their use case and added a suffix that says nothing. Rejected by the product owner.
* *A request record for every command*: duplicates fields and mapping code with no behavior. Kept only where HTTP
  differs.
* *One big service per entity*: hides use cases behind methods, invites shared state and cross-calls. Rejected.

## Trade-offs and exceptions
* More, smaller files. Navigation is by folder and name, which IDEs handle well.
* Binding commands directly means a command's shape is the JSON body of its endpoint; changing a command property is a
  public API change, as changing the request record was.
* Renaming applied in this refactoring changed OpenAPI schema names where types were renamed (listed in the
  refactoring report); JSON payloads, routes, status codes and error codes did not change. No frontend consumed the
  names yet.
* A feature that genuinely needs another structure documents the reason in its PR and, for a file with several
  top-level types, adds an entry with the reason to `ApplicationLayoutTests.MultiTypeFileExceptions`.

## Enforcement
`FeatureConventionTests` (placement, names, no cross-use-case or cross-area references, read from IL) and
`ApplicationLayoutTests` (errors, domain errors, interfaces, application contracts, HTTP request records, one type per
file); IDE0130 for namespaces. Each rule was checked against planted violations.
