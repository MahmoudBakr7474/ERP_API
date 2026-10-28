using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Infrastructure.Persistence;

internal sealed class StockTransferRepository(DbContext db) : IStockTransferRepository
{
    private static readonly Dictionary<string, Error> NoKnownConstraints = new(StringComparer.Ordinal);

    public Task<StockTransferHeader?> FindHeaderAsync(Guid transferId, CancellationToken ct) =>
        db.Set<StockTransfer>().AsNoTracking()
            .Where(x => x.Id == transferId)
            .Select(x => new StockTransferHeader(x.Id, x.SourceWarehouseId, x.DestinationWarehouseId))
            .SingleOrDefaultAsync(ct);

    // UPDLOCK: inside a transaction a second writer of the same transfer waits here, then sees the outcome.
    public async Task<StockTransfer?> GetForUpdateAsync(Guid transferId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var transfer = await db.Set<StockTransfer>()
            .FromSqlInterpolated($"SELECT * FROM [inventory].[StockTransfers] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {transferId}")
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(ct);
        if (transfer is not null && expectedRowVersion is not null)
            db.Entry(transfer).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return transfer;
    }

    public void Add(StockTransfer transfer) => db.Add(transfer);

    public Task<Result> SaveChangesAsync(CancellationToken ct)
    {
        TouchChangedTransfers();
        return DbFailures.SaveAsync(db, NoKnownConstraints, ct);
    }

    // As for documents: a change to lines or history alone must still move the transfer's row version.
    private void TouchChangedTransfers()
    {
        db.ChangeTracker.DetectChanges();
        var changed = db.ChangeTracker.Entries<StockTransferLine>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.TransferId)
            .Concat(db.ChangeTracker.Entries<StockTransferEvent>().Where(e => e.State == EntityState.Added).Select(e => e.Entity.TransferId))
            .ToHashSet();
        foreach (var entry in db.ChangeTracker.Entries<StockTransfer>())
            if (entry.State is EntityState.Unchanged or EntityState.Modified && changed.Contains(entry.Entity.Id))
                entry.Property(x => x.UpdatedAtUtc).IsModified = true;
    }
}
