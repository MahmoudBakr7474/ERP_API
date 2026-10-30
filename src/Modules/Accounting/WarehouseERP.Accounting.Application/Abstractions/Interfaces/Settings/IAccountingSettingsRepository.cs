using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;

/// <summary>The one settings row, seeded by the module's migration.</summary>
public interface IAccountingSettingsRepository
{
    Task<AccountingSettings> GetAsync(CancellationToken ct);

    /// <summary>Tracked; the save fails with a conflict unless the row still has this version.</summary>
    Task<AccountingSettings> GetForUpdateAsync(byte[] expectedRowVersion, CancellationToken ct);

    Task<Result> SaveChangesAsync(CancellationToken ct);
}
