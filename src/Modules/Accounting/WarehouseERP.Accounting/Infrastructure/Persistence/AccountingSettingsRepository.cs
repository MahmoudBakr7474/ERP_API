using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

// The one settings row, seeded by Migration14.
internal sealed class AccountingSettingsRepository(DbContext db) : IAccountingSettingsRepository
{
    public Task<AccountingSettings> GetAsync(CancellationToken ct) =>
        db.Set<AccountingSettings>().AsNoTracking().SingleAsync(x => x.Id == AccountingSettings.SingletonId, ct);

    public async Task<AccountingSettings> GetForUpdateAsync(byte[] expectedRowVersion, CancellationToken ct)
    {
        var settings = await db.Set<AccountingSettings>().SingleAsync(x => x.Id == AccountingSettings.SingletonId, ct);
        db.Entry(settings).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return settings;
    }

    public Task<Result> SaveChangesAsync(CancellationToken ct) => AccountingSaves.SaveAsync(db, ct);
}
