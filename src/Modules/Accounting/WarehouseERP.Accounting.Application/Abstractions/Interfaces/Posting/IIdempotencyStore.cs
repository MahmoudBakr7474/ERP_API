using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;

/// <summary>Posting and approval requests already recorded, by their Idempotency-Key.</summary>
public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> FindAsync(Guid key, CancellationToken ct);

    /// <summary>Saved by the caller's save point, with the posting it records.</summary>
    void Add(IdempotencyRecord record);
}
