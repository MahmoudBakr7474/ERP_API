using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;

/// <summary>Write side of cashboxes and bank accounts; implemented over the shared ErpDbContext.</summary>
public interface ICashAccountRepository
{
    /// <summary>The cash account, untracked, or null.</summary>
    Task<CashAccount?> FindAsync(Guid cashAccountId, CancellationToken ct);

    /// <summary>The cash accounts kept on these ledger accounts, untracked.</summary>
    Task<IReadOnlyList<CashAccount>> FindByLedgerAccountsAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken ct);

    /// <summary>Tracked, or null; with a row version, the save fails with a conflict unless it still has it.</summary>
    Task<CashAccount?> GetForUpdateAsync(Guid cashAccountId, byte[]? expectedRowVersion, CancellationToken ct);

    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    /// <summary>True when another cash account is kept on this ledger account.</summary>
    Task<bool> LedgerAccountUsedAsync(Guid accountId, CancellationToken ct);

    /// <summary>The branch's active cashbox that receives closed POS shifts' cash, untracked, or null.</summary>
    Task<CashAccount?> FindPosCashboxAsync(Guid branchId, CancellationToken ct);

    /// <summary>True when another cashbox of the branch already receives POS cash.</summary>
    Task<bool> PosCashboxTakenAsync(Guid branchId, Guid? exceptCashAccountId, CancellationToken ct);

    void Add(CashAccount cashAccount);

    /// <summary>The save point; a code or ledger account taken at the same moment comes back as a conflict.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
