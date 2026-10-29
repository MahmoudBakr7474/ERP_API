using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Settings;

/// <summary>The one POS settings row, seeded by Migration13.</summary>
public interface IPosSettingsRepository
{
    Task<PosSettings> GetAsync(CancellationToken ct);

    /// <summary>Tracked; the save fails with a conflict unless the row still has this version.</summary>
    Task<PosSettings> GetForUpdateAsync(byte[] expectedRowVersion, CancellationToken ct);

    Task<Result> SaveChangesAsync(CancellationToken ct);
}
