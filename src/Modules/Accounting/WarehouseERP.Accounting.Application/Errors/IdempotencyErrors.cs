using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class IdempotencyErrors
{
    public static readonly Error KeyReused = Error.Conflict(
        "Accounting.Idempotency.KeyReused", "This Idempotency-Key was used for another request; send a new key.");

    public static readonly Error Busy = Error.Conflict(
        "Accounting.Idempotency.Busy", "The same request is still being processed; try again in a moment with the same key.");
}
