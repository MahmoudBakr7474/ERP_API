using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Imports;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Infrastructure.Persistence;

// The location import's unit of work. Branches are loaded as the file names them, with all their warehouses; the
// locations only of their physical warehouses (transit ones are refused). Everything is saved by one SaveChanges (one
// transaction): a code taken by someone else meanwhile, or a location renamed since it was loaded, rolls the whole
// import back with the usual errors.
internal sealed class LocationImportStore(DbContext db) : ILocationImportStore
{
    private const int ChunkSize = 1_000;

    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["IX_WarehouseLocations_WarehouseId_Code"] = WarehouseLocationErrors.CodeTaken,
    };

    public async Task<LocationImportSnapshot> LoadAsync(IReadOnlyCollection<string> branchCodes, CancellationToken ct)
    {
        var branches = new List<Branch>();
        foreach (var chunk in branchCodes.Chunk(ChunkSize))
            branches.AddRange(await db.Set<Branch>().Where(b => chunk.Contains(b.Code)).ToListAsync(ct));
        var branchIds = branches.Select(b => b.Id).ToArray();
        var warehouses = branchIds.Length == 0
            ? []
            : await db.Set<Warehouse>().Where(w => branchIds.Contains(w.BranchId)).ToListAsync(ct);
        var physical = warehouses.Where(w => w.WarehouseType == WarehouseType.Physical).Select(w => w.Id).ToArray();
        var locations = physical.Length == 0
            ? []
            : await db.Set<WarehouseLocation>().Where(l => physical.Contains(l.WarehouseId)).ToListAsync(ct);
        return new LocationImportSnapshot(branches, warehouses, locations);
    }

    public Task<Result> SaveAsync(CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
}
