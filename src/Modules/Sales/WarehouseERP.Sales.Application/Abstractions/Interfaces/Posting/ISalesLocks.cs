namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Posting;

public interface ISalesLocks
{
    /// <summary>
    /// Locks an idempotency key until the transaction ends, so a retry racing its first attempt waits for its outcome;
    /// false when it was not granted in time.
    /// </summary>
    Task<bool> TryLockRequestAsync(Guid idempotencyKey, CancellationToken ct);
}
