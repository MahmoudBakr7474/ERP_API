using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;

/// <summary>Write side of branches; implemented over the shared ErpDbContext (no generic repository).</summary>
public interface IBranchRepository
{
    Task<bool> CodeExistsAsync(Guid companyId, string code, CancellationToken ct);

    /// <summary>Save point for a new branch. A known unique-key violation comes back as its error, not an exception.</summary>
    Task<Result> AddAsync(Branch branch, CancellationToken ct);

    /// <summary>
    /// The branch, tracked for changes, or null. With a row version, the save fails with a conflict unless the
    /// branch still has it.
    /// </summary>
    Task<Branch?> GetForUpdateAsync(Guid branchId, byte[]? expectedRowVersion, CancellationToken ct);

    Task<bool> HasActiveWarehousesAsync(Guid branchId, CancellationToken ct);

    /// <summary>Save point for changes to branches loaded with <see cref="GetForUpdateAsync"/>.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
