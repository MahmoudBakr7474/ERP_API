using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Infrastructure.Persistence;

internal sealed class WarehouseRepository(DbContext db) : IWarehouseRepository
{
    // Unique indexes whose violation is an expected business conflict. Names come from Migration 01.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["IX_Warehouses_BranchId_Code"] = WarehouseErrors.CodeTaken,
        ["IX_WarehouseLocations_WarehouseId_Code"] = WarehouseLocationErrors.CodeTaken,
    };

    public Task<bool> IsBranchActiveAsync(Guid branchId, CancellationToken ct) =>
        db.Set<Branch>().AnyAsync(x => x.Id == branchId && x.IsActive, ct);

    public Task<bool> CodeExistsAsync(Guid branchId, string code, CancellationToken ct) =>
        db.Set<Warehouse>().AnyAsync(x => x.BranchId == branchId && x.Code == code, ct);

    public Task<Result> AddAsync(Warehouse warehouse, CancellationToken ct)
    {
        db.Set<Warehouse>().Add(warehouse);
        return DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
    }

    public async Task<Warehouse?> GetForUpdateAsync(Guid warehouseId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var warehouse = await db.Set<Warehouse>().SingleOrDefaultAsync(x => x.Id == warehouseId, ct);
        if (warehouse is not null && expectedRowVersion is not null)
            // The UPDATE's WHERE uses the original value, so this makes it compare with the caller's version.
            db.Entry(warehouse).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return warehouse;
    }

    public Task<bool> LocationCodeExistsAsync(Guid warehouseId, string code, CancellationToken ct) =>
        db.Set<WarehouseLocation>().AnyAsync(x => x.WarehouseId == warehouseId && x.Code == code, ct);

    public async Task<WarehouseLocation?> GetLocationForUpdateAsync(
        Guid warehouseId, Guid locationId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var location = await db.Set<WarehouseLocation>()
            .SingleOrDefaultAsync(x => x.Id == locationId && x.WarehouseId == warehouseId, ct);
        if (location is not null && expectedRowVersion is not null)
            db.Entry(location).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return location;
    }

    public Task<Result> SaveChangesAsync(CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
}
