using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

internal sealed class CashAccountRepository(DbContext db) : ICashAccountRepository
{
    public Task<CashAccount?> FindAsync(Guid cashAccountId, CancellationToken ct) =>
        db.Set<CashAccount>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == cashAccountId, ct);

    public async Task<IReadOnlyList<CashAccount>> FindByLedgerAccountsAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken ct)
    {
        if (accountIds.Count == 0)
            return [];
        var ids = accountIds.ToArray();
        return await db.Set<CashAccount>().AsNoTracking().Where(x => ids.Contains(x.AccountId)).ToListAsync(ct);
    }

    public async Task<CashAccount?> GetForUpdateAsync(Guid cashAccountId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var cash = await db.Set<CashAccount>().SingleOrDefaultAsync(x => x.Id == cashAccountId, ct);
        if (cash is not null && expectedRowVersion is not null)
            db.Entry(cash).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return cash;
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) => db.Set<CashAccount>().AnyAsync(x => x.Code == code, ct);

    public Task<bool> LedgerAccountUsedAsync(Guid accountId, CancellationToken ct) => db.Set<CashAccount>().AnyAsync(x => x.AccountId == accountId, ct);

    public void Add(CashAccount cashAccount) => db.Add(cashAccount);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => AccountingSaves.SaveAsync(db, ct);
}
