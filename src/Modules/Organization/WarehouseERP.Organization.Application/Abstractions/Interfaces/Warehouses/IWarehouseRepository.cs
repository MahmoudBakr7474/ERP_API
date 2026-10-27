using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;

/// <summary>
/// Write side of warehouses and their locations (one aggregate); implemented over the shared ErpDbContext (no
/// generic repository).
/// </summary>
public interface IWarehouseRepository
{
    Task<bool> IsBranchActiveAsync(Guid branchId, CancellationToken ct);

    Task<bool> CodeExistsAsync(Guid branchId, string code, CancellationToken ct);

    /// <summary>
    /// The use case's save point: the warehouse and its locations in one SaveChanges (one database
    /// transaction). A known unique-key violation comes back as its error, not an exception.
    /// </summary>
    Task<Result> AddAsync(Warehouse warehouse, CancellationToken ct);

    /// <summary>
    /// The warehouse, tracked for changes (its locations not loaded), or null. With a row version, the save fails
    /// with a conflict unless the warehouse still has it.
    /// </summary>
    Task<Warehouse?> GetForUpdateAsync(Guid warehouseId, byte[]? expectedRowVersion, CancellationToken ct);

    Task<bool> LocationCodeExistsAsync(Guid warehouseId, string code, CancellationToken ct);

    /// <summary>
    /// The warehouse's location, tracked for changes, or null. With a row version, the save fails with a conflict
    /// unless the location still has it.
    /// </summary>
    Task<WarehouseLocation?> GetLocationForUpdateAsync(
        Guid warehouseId, Guid locationId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>Save point for changes to warehouses and locations loaded with the methods above.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
