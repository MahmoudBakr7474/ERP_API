using Microsoft.EntityFrameworkCore;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.CardIncidents;
using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Infrastructure.Persistence;

internal sealed class CardIncidentRepository(DbContext db) : ICardIncidentRepository
{
    public Task<PosCardIncident?> FindByKeyAsync(Guid idempotencyKey, CancellationToken ct) =>
        db.Set<PosCardIncident>().SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);

    public Task<Guid?> FindWarehouseAsync(Guid incidentId, CancellationToken ct) =>
        db.Set<PosCardIncident>().AsNoTracking().Where(x => x.Id == incidentId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    public async Task<PosCardIncident?> GetForUpdateAsync(Guid incidentId, byte[] expectedRowVersion, CancellationToken ct)
    {
        var incident = await db.Set<PosCardIncident>().SingleOrDefaultAsync(x => x.Id == incidentId, ct);
        if (incident is not null)
            db.Entry(incident).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return incident;
    }

    public void Add(PosCardIncident incident) => db.Add(incident);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => PosSaves.SaveAsync(db, ct);
}
