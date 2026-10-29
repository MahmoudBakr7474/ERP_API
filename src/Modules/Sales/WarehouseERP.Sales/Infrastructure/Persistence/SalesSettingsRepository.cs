using Microsoft.EntityFrameworkCore;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Infrastructure.Persistence;

// The one settings row, seeded by Migration10.
internal sealed class SalesSettingsRepository(DbContext db) : ISalesSettingsRepository
{
    public Task<SalesSettings> GetAsync(CancellationToken ct) =>
        db.Set<SalesSettings>().AsNoTracking().SingleAsync(x => x.Id == SalesSettings.SingletonId, ct);

    public async Task<SalesSettings> GetForUpdateAsync(byte[] expectedRowVersion, CancellationToken ct)
    {
        var settings = await db.Set<SalesSettings>().SingleAsync(x => x.Id == SalesSettings.SingletonId, ct);
        db.Entry(settings).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return settings;
    }

    public Task<Result> SaveChangesAsync(CancellationToken ct) => SalesSaves.SaveAsync(db, ct);
}
