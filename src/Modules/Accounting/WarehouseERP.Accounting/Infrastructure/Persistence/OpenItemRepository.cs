using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

internal sealed class OpenItemRepository(DbContext db) : IOpenItemRepository
{
    public async Task<IReadOnlyList<OpenItem>> FindAsync(IReadOnlyCollection<Guid> openItemIds, CancellationToken ct)
    {
        if (openItemIds.Count == 0)
            return [];
        var ids = openItemIds.ToArray();
        return await db.Set<OpenItem>().AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OpenItem>> GetForUpdateAsync(IReadOnlyCollection<Guid> openItemIds, CancellationToken ct)
    {
        if (openItemIds.Count == 0)
            return [];
        var ids = openItemIds.ToArray();
        return await db.Set<OpenItem>().Where(x => ids.Contains(x.Id)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OpenItem>> ListOfJournalForUpdateAsync(Guid journalId, CancellationToken ct) =>
        await db.Set<OpenItem>().Where(x => x.JournalEntryId == journalId).ToListAsync(ct);

    public Task<bool> HasActiveAllocationsAsync(Guid journalId, CancellationToken ct) =>
        (from allocation in db.Set<Allocation>()
         join item in db.Set<OpenItem>() on allocation.DebitItemId equals item.Id
         where allocation.IsActive && item.JournalEntryId == journalId
         select allocation.Id)
        .Concat(from allocation in db.Set<Allocation>()
                join item in db.Set<OpenItem>() on allocation.CreditItemId equals item.Id
                where allocation.IsActive && item.JournalEntryId == journalId
                select allocation.Id)
        .AnyAsync(ct);

    public async Task<IReadOnlyList<OpenItem>> ListOpenForUpdateAsync(PartyType partyType, Guid partyId, CancellationToken ct) =>
        await db.Set<OpenItem>().Where(x => x.PartyType == partyType && x.PartyId == partyId && x.OpenAmount != 0m)
            .OrderBy(x => x.DueDate).ThenBy(x => x.DocumentDate).ToListAsync(ct);

    public Task<Allocation?> FindAllocationAsync(Guid allocationId, CancellationToken ct) =>
        db.Set<Allocation>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == allocationId, ct);

    public Task<Allocation?> GetAllocationForUpdateAsync(Guid allocationId, CancellationToken ct) =>
        db.Set<Allocation>().SingleOrDefaultAsync(x => x.Id == allocationId, ct);

    public void Add(OpenItem item) => db.Add(item);

    public void Add(Allocation allocation) => db.Add(allocation);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => AccountingSaves.SaveAsync(db, ct);
}
