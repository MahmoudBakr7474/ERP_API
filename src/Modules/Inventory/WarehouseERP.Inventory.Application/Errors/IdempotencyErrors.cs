using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Errors;

public static class IdempotencyErrors
{
    public static readonly Error KeyReused = Error.Conflict(
        "Inventory.Idempotency.KeyReused", "This Idempotency-Key was used for another request; send a new key.");
}
