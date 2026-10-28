using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Infrastructure.Persistence;

internal sealed class StockDocumentRepository(DbContext db) : IStockDocumentRepository
{
    // Unique indexes whose violation is an expected business conflict. Names come from Migration04.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["UX_StockDocuments_OneReversal"] = StockDocumentErrors.AlreadyReversed,
    };

    public Task<StockDocumentHeader?> FindHeaderAsync(Guid documentId, CancellationToken ct) =>
        db.Set<StockDocument>().AsNoTracking()
            .Where(x => x.Id == documentId)
            .Select(x => new StockDocumentHeader(
                x.Id, x.WarehouseId, x.DocumentType,
                db.Set<StockDocument>().Where(o => o.Id == x.ReversalOfDocumentId).Select(o => (StockDocumentType?)o.DocumentType).FirstOrDefault()))
            .SingleOrDefaultAsync(ct);

    // UPDLOCK: inside a posting's transaction a second writer of the same document waits here, then sees the outcome.
    public async Task<StockDocument?> GetForUpdateAsync(Guid documentId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var document = await db.Set<StockDocument>()
            .FromSqlInterpolated($"SELECT * FROM [inventory].[StockDocuments] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {documentId}")
            .Include(x => x.Lines).ThenInclude(x => x.Allocations)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        if (document is not null && expectedRowVersion is not null)
            // The UPDATE's WHERE uses the original value, so this makes it compare with the caller's version.
            db.Entry(document).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return document;
    }

    public Task<bool> HasPostedBeyondOpeningAsync(Guid warehouseId, CancellationToken ct) =>
        db.Set<StockDocument>().AnyAsync(x =>
            x.WarehouseId == warehouseId
            && x.Status == StockDocumentStatus.Posted
            && x.DocumentType != StockDocumentType.OpeningBalance
            && !(x.DocumentType == StockDocumentType.Reversal
                 && db.Set<StockDocument>().Any(o => o.Id == x.ReversalOfDocumentId && o.DocumentType == StockDocumentType.OpeningBalance)),
            ct);

    public Task<bool> HasReversalAsync(Guid documentId, CancellationToken ct) =>
        db.Set<StockDocument>().AnyAsync(x => x.ReversalOfDocumentId == documentId && x.Status != StockDocumentStatus.Cancelled, ct);

    public Task<bool> HasSourceAsync(Guid sourceDocumentId, CancellationToken ct) =>
        db.Set<StockDocument>().AnyAsync(x => x.SourceDocumentId == sourceDocumentId, ct);

    public void Add(StockDocument document) => db.Add(document);

    public Task<Result> SaveChangesAsync(CancellationToken ct)
    {
        TouchChangedDocuments();
        return DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
    }

    // Changing only lines or history would leave the document row, and so its row version, untouched; an editor working
    // from an older version would not notice. Marking a real column modified makes the save write the row, check the
    // version and renew it. (A caller's expected row version alone counts as a change EF has nothing to write for.)
    private void TouchChangedDocuments()
    {
        db.ChangeTracker.DetectChanges();
        var changed = db.ChangeTracker.Entries<StockDocumentLine>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.DocumentId)
            .Concat(db.ChangeTracker.Entries<StockDocumentEvent>().Where(e => e.State == EntityState.Added).Select(e => e.Entity.DocumentId))
            .ToHashSet();
        foreach (var entry in db.ChangeTracker.Entries<StockDocument>())
            if (entry.State is EntityState.Unchanged or EntityState.Modified && changed.Contains(entry.Entity.Id))
                entry.Property(x => x.UpdatedAtUtc).IsModified = true;
    }
}
