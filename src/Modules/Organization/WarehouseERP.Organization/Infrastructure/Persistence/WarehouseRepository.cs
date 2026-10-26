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
}
