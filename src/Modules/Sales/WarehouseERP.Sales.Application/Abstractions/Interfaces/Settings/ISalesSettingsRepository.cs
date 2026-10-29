using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Settings;

/// <summary>The one row of sales settings.</summary>
public interface ISalesSettingsRepository
{
    /// <summary>The settings as they are now, untracked.</summary>
    Task<SalesSettings> GetAsync(CancellationToken ct);

    /// <summary>The settings, tracked; the save fails with a conflict unless they still have the row version.</summary>
    Task<SalesSettings> GetForUpdateAsync(byte[] expectedRowVersion, CancellationToken ct);

    Task<Result> SaveChangesAsync(CancellationToken ct);
}
