using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Errors;

public static class IdempotencyErrors
{
    public static readonly Error KeyReused = Error.Conflict(
        "Pos.Idempotency.KeyReused", "This Idempotency-Key was used for another request; send a new key.");

    public static readonly Error Busy = Error.Conflict(
        "Pos.Idempotency.Busy", "The same request is still being processed; try again in a moment with the same key.");
}
