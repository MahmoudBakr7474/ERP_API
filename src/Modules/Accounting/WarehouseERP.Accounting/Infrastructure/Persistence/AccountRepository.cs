using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

internal sealed class AccountRepository(DbContext db) : IAccountRepository
{
    public Task<Account?> FindAsync(Guid accountId, CancellationToken ct) =>
        db.Set<Account>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId, ct);

    public async Task<IReadOnlyList<Account>> FindManyAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken ct)
    {
        if (accountIds.Count == 0)
            return [];
        var ids = accountIds.ToArray();
        return await db.Set<Account>().AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(ct);
    }

    public async Task<Account?> GetForUpdateAsync(Guid accountId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var account = await db.Set<Account>().SingleOrDefaultAsync(x => x.Id == accountId, ct);
        if (account is not null && expectedRowVersion is not null)
            db.Entry(account).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return account;
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) => db.Set<Account>().AnyAsync(x => x.Code == code, ct);

    public Task<bool> AnyAsync(CancellationToken ct) => db.Set<Account>().AnyAsync(ct);

    public Task<bool> HasChildrenAsync(Guid accountId, bool activeOnly, CancellationToken ct) =>
        db.Set<Account>().AnyAsync(x => x.ParentId == accountId && (!activeOnly || x.IsActive), ct);

    public Task<bool> HasLinesAsync(Guid accountId, CancellationToken ct) => db.Set<JournalLine>().AnyAsync(x => x.AccountId == accountId, ct);

    public async Task<decimal> PostedBalanceAsync(Guid accountId, CancellationToken ct) =>
        await (from line in db.Set<JournalLine>()
               join journal in db.Set<JournalEntry>() on line.JournalEntryId equals journal.Id
               where line.AccountId == accountId && journal.Status == JournalStatus.Posted
               select line.Debit - line.Credit).SumAsync(ct);

    public Task<bool> IsMappedAsync(Guid accountId, CancellationToken ct) => db.Set<AccountMapping>().AnyAsync(x => x.AccountId == accountId, ct);

    public void Add(Account account) => db.Add(account);

    public void Remove(Account account) => db.Remove(account);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => AccountingSaves.SaveAsync(db, ct);
}
