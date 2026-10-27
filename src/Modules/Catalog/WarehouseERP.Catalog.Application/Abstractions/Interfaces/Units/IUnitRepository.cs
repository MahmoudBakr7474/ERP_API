using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Units;

/// <summary>Write side of units; implemented over the shared ErpDbContext (no generic repository).</summary>
public interface IUnitRepository
{
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    /// <summary>Save point for a new unit. A known unique-key violation comes back as its error, not an exception.</summary>
    Task<Result> AddAsync(Unit unit, CancellationToken ct);

    /// <summary>
    /// The unit, tracked for changes, or null. With a row version, the save fails with a conflict unless the unit
    /// still has it.
    /// </summary>
    Task<Unit?> GetForUpdateAsync(Guid unitId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>True while an active product keeps stock in this unit or holds it as an active product unit.</summary>
    Task<bool> IsUsedByActiveProductsAsync(Guid unitId, CancellationToken ct);

    /// <summary>Save point for changes to units loaded with <see cref="GetForUpdateAsync"/>.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
