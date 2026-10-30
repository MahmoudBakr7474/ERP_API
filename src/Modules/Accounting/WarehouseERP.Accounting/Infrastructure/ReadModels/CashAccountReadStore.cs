using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Contracts.CashAccounts;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Accounting.Infrastructure.ReadModels;

// A cash account's balance is its ledger account's posted balance, summed from the lines every time.
internal sealed class CashAccountReadStore(DbContext db, IBranchDirectory branches) : ICashAccountReadStore
{
    public async Task<CashAccountResponse?> GetAsync(Guid cashAccountId, CancellationToken ct) =>
        (await MapAsync(await db.Set<CashAccount>().AsNoTracking().Where(x => x.Id == cashAccountId).ToListAsync(ct), null, ct)).SingleOrDefault();

    public async Task<IReadOnlyList<CashAccountResponse>> ListAsync(
        CashAccountKind? kind, Guid? branchId, bool? isActive, DateOnly? asOfDate, CancellationToken ct)
    {
        var query = db.Set<CashAccount>().AsNoTracking();
        if (kind is { } k)
            query = query.Where(x => x.Kind == k);
        if (branchId is { } branch)
            query = query.Where(x => x.BranchId == branch);
        if (isActive is { } active)
            query = query.Where(x => x.IsActive == active);
        return await MapAsync(await query.OrderBy(x => x.Code).ToListAsync(ct), asOfDate, ct);
    }

    private async Task<IReadOnlyList<CashAccountResponse>> MapAsync(List<CashAccount> cash, DateOnly? asOfDate, CancellationToken ct)
    {
        if (cash.Count == 0)
            return [];
        var accountIds = cash.Select(c => c.AccountId).ToArray();
        var accounts = await db.Set<Account>().AsNoTracking().Where(x => accountIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => new { x.Code, x.NameAr }, ct);
        var balances = await (from line in db.Set<JournalLine>()
                              join journal in db.Set<JournalEntry>() on line.JournalEntryId equals journal.Id
                              where accountIds.Contains(line.AccountId) && journal.Status == JournalStatus.Posted
                                    && (asOfDate == null || journal.PostingDate <= asOfDate)
                              group line by line.AccountId into g
                              select new { AccountId = g.Key, Balance = g.Sum(l => l.Debit - l.Credit) })
            .ToDictionaryAsync(x => x.AccountId, x => x.Balance, ct);
        var branchInfo = (await branches.FindManyAsync(cash.Select(c => c.BranchId).Distinct().ToList(), ct)).ToDictionary(b => b.Id);
        return cash.Select(c =>
        {
            var account = accounts.GetValueOrDefault(c.AccountId);
            var branch = branchInfo.GetValueOrDefault(c.BranchId);
            return new CashAccountResponse(c.Id, c.Code, c.Name, c.Kind, c.BranchId, branch?.Code, branch?.NameAr, c.AccountId, account?.Code ?? "",
                account?.NameAr ?? "", c.BankName, c.BankAccountNumber, c.AllowsOverdraft, c.IsActive, balances.GetValueOrDefault(c.AccountId),
                c.CreatedAtUtc, Convert.ToBase64String(c.RowVersion));
        }).ToList();
    }
}
