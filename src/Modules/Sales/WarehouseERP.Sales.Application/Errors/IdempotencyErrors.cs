using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Errors;

public static class IdempotencyErrors
{
    public static readonly Error KeyReused = Error.Conflict(
        "Sales.Idempotency.KeyReused", "This Idempotency-Key was used for another request; send a new key.");

    public static readonly Error Busy = Error.Conflict(
        "Sales.Idempotency.Busy", "The same request is still being processed; try again in a moment.");
}
