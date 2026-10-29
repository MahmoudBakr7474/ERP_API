using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Posting;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> FindAsync(Guid key, CancellationToken ct);

    /// <summary>Saved with the change it records, by the use case's save point.</summary>
    void Add(IdempotencyRecord record);
}
