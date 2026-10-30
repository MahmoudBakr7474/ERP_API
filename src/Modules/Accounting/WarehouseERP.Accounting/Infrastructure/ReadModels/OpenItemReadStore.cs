using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Infrastructure.ReadModels;

internal sealed class OpenItemReadStore(DbContext db) : IOpenItemReadStore
{
    public async Task<PagedResult<OpenItem>> ListAsync(OpenItemFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<OpenItem>().AsNoTracking();
        if (filter.PartyType is { } partyType)
            query = query.Where(x => x.PartyType == partyType);
        if (filter.PartyId is { } partyId)
            query = query.Where(x => x.PartyId == partyId);
        if (filter.AccountId is { } accountId)
            query = query.Where(x => x.AccountId == accountId);
        if (filter.OpenOnly)
            query = query.Where(x => x.OpenAmount != 0m);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.DueDate).ThenBy(x => x.DocumentDate).ThenBy(x => x.DocumentNumber)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<OpenItem>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<Allocation>> AllocationsOfAsync(IReadOnlyCollection<Guid> openItemIds, CancellationToken ct)
    {
        if (openItemIds.Count == 0)
            return [];
        var ids = openItemIds.ToArray();
        return await db.Set<Allocation>().AsNoTracking()
            .Where(x => x.IsActive && (ids.Contains(x.DebitItemId) || ids.Contains(x.CreditItemId)))
            .OrderBy(x => x.AllocatedOn).ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }
}
