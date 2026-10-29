using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.CardIncidents;

/// <summary>Write side of card incidents.</summary>
public interface ICardIncidentRepository
{
    /// <summary>The incident of a checkout's Idempotency-Key, tracked, or null.</summary>
    Task<PosCardIncident?> FindByKeyAsync(Guid idempotencyKey, CancellationToken ct);

    /// <summary>The incident's warehouse, for the scope check, or null.</summary>
    Task<Guid?> FindWarehouseAsync(Guid incidentId, CancellationToken ct);

    /// <summary>Tracked, or null; the save fails with a conflict unless it still has the row version.</summary>
    Task<PosCardIncident?> GetForUpdateAsync(Guid incidentId, byte[] expectedRowVersion, CancellationToken ct);

    void Add(PosCardIncident incident);

    Task<Result> SaveChangesAsync(CancellationToken ct);
}
