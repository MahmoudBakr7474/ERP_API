# ADR 0003 — Result pattern and the HTTP error contract

Status: accepted (2026-10-26)

## Decision
* Expected failures are `Result`/`Result<T>` carrying an `Error { Code, Message, Type, ValidationErrors }`
  (SharedKernel). Exceptions are for the unexpected.
* Codes are stable and spelled `Module.Subject.Reason` (`Catalog.Product.SkuTaken`); clients branch on the code,
  never on the message. Domain errors live in the Domain (`WarehouseDomainErrors`), use-case errors in the
  Application by business area (`Application/Errors/WarehouseErrors`, ADR 0011), shared ones in `CommonErrors`.
* `ErrorType` → status, in one place (`ResultHttpMapping`): Validation 400, NotFound 404, Conflict 409,
  BusinessRule 422, Unauthorized 401, Forbidden 403.
* A command that names a missing or inactive referenced entity (a branch, a unit) is 422 BusinessRule; 404 is
  only for the resource the URL addresses.
* Every error response is ProblemDetails (RFC 9457) with `errorCode`, `traceId`, `correlationId`, and `errors[]`
  (`{ property, code, message }`, property in camelCase) for validation. Framework responses (401/403/404/405)
  get `Common.*` codes; unexpected exceptions get `Common.Unexpected` with no detail.
* The Application never returns `IResult`/`IActionResult`; Presentation maps with `ToHttpResult`.

## Consequences
React can switch on `errorCode` and bind `errors[]` to form fields. Adding an error is adding a static `Error`;
adding an `ErrorType` means adding its status to the mapping (the switch throws on an unmapped type).
