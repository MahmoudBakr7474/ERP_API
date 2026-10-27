using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Infrastructure.Persistence;

internal sealed class BranchRepository(DbContext db) : IBranchRepository
{
    // Unique indexes whose violation is an expected business conflict. Names come from Migration 01.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["IX_Branches_CompanyId_Code"] = BranchErrors.CodeTaken,
    };

    public Task<bool> CodeExistsAsync(Guid companyId, string code, CancellationToken ct) =>
        db.Set<Branch>().AnyAsync(x => x.CompanyId == companyId && x.Code == code, ct);

    public Task<Result> AddAsync(Branch branch, CancellationToken ct)
    {
        db.Set<Branch>().Add(branch);
        return DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
    }

    public async Task<Branch?> GetForUpdateAsync(Guid branchId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var branch = await db.Set<Branch>().SingleOrDefaultAsync(x => x.Id == branchId, ct);
        if (branch is not null && expectedRowVersion is not null)
            // The UPDATE's WHERE uses the original value, so this makes it compare with the caller's version.
            db.Entry(branch).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return branch;
    }

    public Task<bool> HasActiveWarehousesAsync(Guid branchId, CancellationToken ct) =>
        db.Set<Warehouse>().AnyAsync(x => x.BranchId == branchId && x.IsActive, ct);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
}
