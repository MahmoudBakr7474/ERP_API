using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Posting;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> FindAsync(Guid key, CancellationToken ct);

    /// <summary>Saved with the change it records, by the use case's save point.</summary>
    void Add(IdempotencyRecord record);
}
