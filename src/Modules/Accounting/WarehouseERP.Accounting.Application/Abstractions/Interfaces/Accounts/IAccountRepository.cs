using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;

/// <summary>Write side of the chart of accounts; implemented over the shared ErpDbContext.</summary>
public interface IAccountRepository
{
    /// <summary>The account, untracked, or null.</summary>
    Task<Account?> FindAsync(Guid accountId, CancellationToken ct);

    /// <summary>The accounts among these ids that exist, untracked.</summary>
    Task<IReadOnlyList<Account>> FindManyAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken ct);

    /// <summary>Tracked, or null; with a row version, the save fails with a conflict unless it still has it.</summary>
    Task<Account?> GetForUpdateAsync(Guid accountId, byte[]? expectedRowVersion, CancellationToken ct);

    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    /// <summary>True once the chart has any account.</summary>
    Task<bool> AnyAsync(CancellationToken ct);

    Task<bool> HasChildrenAsync(Guid accountId, bool activeOnly, CancellationToken ct);

    /// <summary>True when any journal line, posted or not, names the account.</summary>
    Task<bool> HasLinesAsync(Guid accountId, CancellationToken ct);

    /// <summary>Posted debits minus posted credits.</summary>
    Task<decimal> PostedBalanceAsync(Guid accountId, CancellationToken ct);

    /// <summary>
    /// The lowest posted balance (debits minus credits) at the end of any day from <paramref name="fromDate"/> on: what a
    /// change dated that day may take the account down by without leaving any later day below zero.
    /// </summary>
    Task<decimal> LowestBalanceFromAsync(Guid accountId, DateOnly fromDate, CancellationToken ct);

    Task<bool> IsMappedAsync(Guid accountId, CancellationToken ct);

    void Add(Account account);

    void Remove(Account account);

    /// <summary>The save point; a code taken at the same moment comes back as a conflict.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
