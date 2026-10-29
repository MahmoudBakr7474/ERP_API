using Microsoft.EntityFrameworkCore;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Infrastructure.Persistence;

// The one settings row, seeded by Migration13.
internal sealed class PosSettingsRepository(DbContext db) : IPosSettingsRepository
{
    public Task<PosSettings> GetAsync(CancellationToken ct) =>
        db.Set<PosSettings>().AsNoTracking().SingleAsync(x => x.Id == PosSettings.SingletonId, ct);

    public async Task<PosSettings> GetForUpdateAsync(byte[] expectedRowVersion, CancellationToken ct)
    {
        var settings = await db.Set<PosSettings>().SingleAsync(x => x.Id == PosSettings.SingletonId, ct);
        db.Entry(settings).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return settings;
    }

    public Task<Result> SaveChangesAsync(CancellationToken ct) => PosSaves.SaveAsync(db, ct);
}
