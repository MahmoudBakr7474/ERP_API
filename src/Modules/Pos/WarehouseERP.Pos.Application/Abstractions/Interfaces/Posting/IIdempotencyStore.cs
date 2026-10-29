using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Posting;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> FindAsync(Guid key, CancellationToken ct);

    /// <summary>Saved with the sale or return it records, by the use case's save point.</summary>
    void Add(IdempotencyRecord record);
}
