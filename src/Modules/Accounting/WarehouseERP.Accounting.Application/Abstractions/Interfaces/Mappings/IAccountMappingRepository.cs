using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;

/// <summary>Posting-role mappings; implemented over the shared ErpDbContext.</summary>
public interface IAccountMappingRepository
{
    /// <summary>Every mapped role, untracked.</summary>
    Task<IReadOnlyList<AccountMapping>> ListAsync(CancellationToken ct);

    /// <summary>The role's mapping, untracked, or null when it is not mapped.</summary>
    Task<AccountMapping?> FindAsync(PostingRole role, CancellationToken ct);

    /// <summary>Tracked, or null; with a row version, the save fails with a conflict unless it still has it.</summary>
    Task<AccountMapping?> GetForUpdateAsync(PostingRole role, byte[]? expectedRowVersion, CancellationToken ct);

    Task<bool> AnyAsync(CancellationToken ct);

    void Add(AccountMapping mapping);

    void Remove(AccountMapping mapping);

    Task<Result> SaveChangesAsync(CancellationToken ct);
}
