using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Contracts.Accounts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Infrastructure.ReadModels;

internal sealed class AccountReadStore(DbContext db) : IAccountReadStore
{
    public async Task<AccountResponse?> GetAsync(Guid accountId, CancellationToken ct)
    {
        var account = await db.Set<Account>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId, ct);
        if (account is null)
            return null;
        var parent = account.ParentId is { } parentId
            ? await db.Set<Account>().AsNoTracking().Where(x => x.Id == parentId).Select(x => new { x.Code, x.NameAr }).SingleOrDefaultAsync(ct)
            : null;
        var hasChildren = await db.Set<Account>().AnyAsync(x => x.ParentId == accountId, ct);
        var hasLines = await db.Set<JournalLine>().AnyAsync(x => x.AccountId == accountId, ct);
        var roles = await db.Set<AccountMapping>().AsNoTracking().Where(x => x.AccountId == accountId).OrderBy(x => x.Role)
            .Select(x => x.Role).ToListAsync(ct);
        return new AccountResponse(account.Id, account.Code, account.NameAr, account.NameEn, account.ParentId, parent?.Code, parent?.NameAr,
            account.Level, account.Type, account.Section, account.IsPosting, account.AllowsManualPosting, account.PartyType, account.IsActive,
            hasChildren, hasLines, roles, account.CreatedAtUtc, account.UpdatedAtUtc, Convert.ToBase64String(account.RowVersion));
    }

    public async Task<PagedResult<AccountSummaryResponse>> ListAsync(AccountFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<Account>().AsNoTracking();
        if (filter.Search is { } search)
            query = query.Where(x => x.Code.Contains(search) || x.NameAr.Contains(search) || (x.NameEn != null && x.NameEn.Contains(search)));
        if (filter.Type is { } type)
            query = query.Where(x => x.Type == type);
        if (filter.IsActive is { } isActive)
            query = query.Where(x => x.IsActive == isActive);
        if (filter.IsPosting is { } isPosting)
            query = query.Where(x => x.IsPosting == isPosting);
        if (filter.ParentId is { } parentId)
            query = query.Where(x => x.ParentId == parentId);

        var total = await query.CountAsync(ct);
        var items = await Summaries(query.OrderBy(x => x.Code).Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);
        return new PagedResult<AccountSummaryResponse>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<AccountSummaryResponse>> ListAllAsync(CancellationToken ct) =>
        await Summaries(db.Set<Account>().AsNoTracking().OrderBy(x => x.Code)).ToListAsync(ct);

    private static IQueryable<AccountSummaryResponse> Summaries(IQueryable<Account> accounts) =>
        accounts.Select(x => new AccountSummaryResponse(x.Id, x.Code, x.NameAr, x.NameEn, x.ParentId, x.Level, x.Type, x.Section,
            x.IsPosting, x.AllowsManualPosting, x.PartyType, x.IsActive));
}
