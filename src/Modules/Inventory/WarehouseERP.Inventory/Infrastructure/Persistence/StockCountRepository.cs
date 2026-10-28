using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Infrastructure.Persistence;

internal sealed class StockCountRepository(DbContext db) : IStockCountRepository
{
    // Unique indexes whose violation is an expected business conflict. Names come from Migration05.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["UX_StockCountScopes_ActiveWarehouse"] = StockCountErrors.ScopeOverlaps,
        ["UX_StockCountScopes_ActiveLocation"] = StockCountErrors.ScopeOverlaps,
    };

    public async Task<Guid?> FindWarehouseIdAsync(Guid countId, CancellationToken ct) =>
        await db.Set<StockCount>().AsNoTracking().Where(x => x.Id == countId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    public async Task<StockCount?> GetForUpdateAsync(Guid countId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var count = await db.Set<StockCount>()
            .FromSqlInterpolated($"SELECT * FROM [inventory].[StockCounts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {countId}")
            .Include(x => x.Scopes)
            .Include(x => x.Lines)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        if (count is not null && expectedRowVersion is not null)
            db.Entry(count).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return count;
    }

    public Task<bool> OverlapsActiveScopeAsync(StockCount count, CancellationToken ct)
    {
        var scopes = db.Set<StockCountScope>().Where(s => s.IsActive && s.WarehouseId == count.WarehouseId && s.CountId != count.Id);
        if (count.IsWholeWarehouse)
            return scopes.AnyAsync(ct);
        var locations = count.Scopes.Where(s => s.LocationId.HasValue).Select(s => s.LocationId!.Value).ToArray();
        return scopes.AnyAsync(s => s.LocationId == null || locations.Contains(s.LocationId.Value), ct);
    }

    public async Task<IReadOnlyList<CountSnapshotLine>> ReadScopeBalancesAsync(StockCount count, CancellationToken ct)
    {
        var balances = db.Set<StockBalance>().AsNoTracking()
            .Where(b => b.WarehouseId == count.WarehouseId && b.StockStatus == StockStatus.Available && b.LotId == null && b.Quantity != 0);
        if (!count.IsWholeWarehouse)
        {
            var locations = count.Scopes.Where(s => s.LocationId.HasValue).Select(s => s.LocationId!.Value).ToArray();
            balances = balances.Where(b => locations.Contains(b.LocationId));
        }
        return await balances
            .OrderBy(b => b.LocationId).ThenBy(b => b.ProductId)
            .Select(b => new CountSnapshotLine(b.ProductId, b.LocationId, b.Quantity))
            .ToListAsync(ct);
    }

    public void Add(StockCount count) => db.Add(count);

    public Task<Result> SaveChangesAsync(CancellationToken ct)
    {
        TouchChangedCounts();
        return DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
    }

    private void TouchChangedCounts()
    {
        db.ChangeTracker.DetectChanges();
        var changed = db.ChangeTracker.Entries<StockCountLine>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.CountId)
            .Concat(db.ChangeTracker.Entries<StockCountScope>().Where(e => e.State == EntityState.Modified).Select(e => e.Entity.CountId))
            .Concat(db.ChangeTracker.Entries<StockCountEvent>().Where(e => e.State == EntityState.Added).Select(e => e.Entity.CountId))
            .ToHashSet();
        foreach (var entry in db.ChangeTracker.Entries<StockCount>())
            if (entry.State is EntityState.Unchanged or EntityState.Modified && changed.Contains(entry.Entity.Id))
                entry.Property(x => x.UpdatedAtUtc).IsModified = true;
    }
}
