using Microsoft.EntityFrameworkCore;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.ReadModels;

// What Accounting may learn about stock value: posted documents and their cost entries, read without locks.
internal sealed class InventoryAccountingSource(DbContext db) : IInventoryAccountingSource
{
    private static readonly StockDocumentType[] OwnTypes =
    [
        StockDocumentType.OpeningBalance, StockDocumentType.Receipt, StockDocumentType.Issue, StockDocumentType.Adjustment,
        StockDocumentType.Reversal, StockDocumentType.CountAdjustment,
    ];

    public async Task<IReadOnlyList<InventoryPostedDocument>> ListPostedAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct) =>
        await db.Set<StockDocument>().AsNoTracking()
            .Where(x => x.Status == StockDocumentStatus.Posted && OwnTypes.Contains(x.DocumentType)
                        && x.PostedAtUtc >= fromUtc && x.PostedAtUtc < toUtc)
            .OrderBy(x => x.PostedAtUtc).ThenBy(x => x.Id)
            .Select(x => new InventoryPostedDocument(x.Id, x.DocumentType, x.Number!, x.PostedAtUtc!.Value))
            .ToListAsync(ct);

    public async Task<InventoryDocumentValue?> GetDocumentAsync(Guid documentId, CancellationToken ct)
    {
        var document = await db.Set<StockDocument>().AsNoTracking()
            .Where(x => x.Id == documentId && x.Status == StockDocumentStatus.Posted)
            .Select(x => new { x.Id, x.DocumentType, x.Number, x.BranchId, x.PostedAtUtc, x.AdjustmentReason, x.ReversalOfDocumentId })
            .SingleOrDefaultAsync(ct);
        if (document is null)
            return null;
        var reversed = document.ReversalOfDocumentId is { } reversedId
            ? await db.Set<StockDocument>().AsNoTracking().Where(x => x.Id == reversedId)
                .Select(x => new { x.DocumentType, x.Number, x.AdjustmentReason }).SingleAsync(ct)
            : null;
        var entries = await db.Set<InventoryCostEntry>().AsNoTracking()
            .Where(x => x.DocumentId == documentId)
            .Select(x => x.ValueDelta)
            .ToListAsync(ct);
        return new InventoryDocumentValue(
            document.Id, document.DocumentType, document.Number!, document.BranchId, document.PostedAtUtc!.Value,
            reversed?.AdjustmentReason ?? document.AdjustmentReason, reversed?.DocumentType, reversed?.Number,
            entries.Where(v => v > 0).Sum(), -entries.Where(v => v < 0).Sum());
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetDocumentValuesAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken ct)
    {
        if (documentIds.Count == 0)
            return new Dictionary<Guid, decimal>();
        var ids = documentIds.Distinct().ToArray();
        return await db.Set<InventoryCostEntry>().AsNoTracking()
            .Where(x => ids.Contains(x.DocumentId))
            .GroupBy(x => x.DocumentId)
            .Select(g => new { g.Key, Value = g.Sum(x => x.ValueDelta) })
            .ToDictionaryAsync(x => x.Key, x => x.Value, ct);
    }

    public async Task<IReadOnlyList<InventoryDocumentPosting>> GetPostingsAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken ct)
    {
        if (documentIds.Count == 0)
            return [];
        var ids = documentIds.Distinct().ToArray();
        return await db.Set<InventoryCostEntry>().AsNoTracking()
            .Where(x => ids.Contains(x.DocumentId))
            .GroupBy(x => x.DocumentId)
            .Select(g => new InventoryDocumentPosting(g.Key, g.Min(x => x.PostedAtUtc), g.Sum(x => x.ValueDelta)))
            .ToListAsync(ct);
    }

    public async Task<decimal> ValuationBeforeAsync(DateTime beforeUtc, CancellationToken ct) =>
        await db.Set<InventoryCostEntry>().AsNoTracking()
            .Where(x => x.PostedAtUtc < beforeUtc)
            .SumAsync(x => (decimal?)x.ValueDelta, ct) ?? 0m;
}
