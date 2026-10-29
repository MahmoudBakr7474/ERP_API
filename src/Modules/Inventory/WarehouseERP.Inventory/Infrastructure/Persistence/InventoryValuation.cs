using Microsoft.EntityFrameworkCore;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence;

// Valuation rows are locked one product at a time in ascending order (UPDLOCK, HOLDLOCK also covers a product not yet
// valued), after the stock rows, so postings of the same product queue in one order. Changes save with the posting.
internal sealed class InventoryValuation(DbContext db) : IInventoryValuation
{
    public async Task<IReadOnlyDictionary<Guid, InventoryCostState>> LockAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct)
    {
        var states = new Dictionary<Guid, InventoryCostState>();
        foreach (var productId in productIds.Distinct().Order())
        {
            var state = await db.Set<InventoryCostState>()
                .FromSqlInterpolated($"SELECT * FROM [inventory].[InventoryCostStates] WITH (UPDLOCK, HOLDLOCK) WHERE [ProductId] = {productId}")
                .SingleOrDefaultAsync(ct);
            if (state is not null)
                states.Add(productId, state);
        }
        return states;
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetLineValuesAsync(Guid documentId, CancellationToken ct) =>
        await db.Set<InventoryCostEntry>().AsNoTracking()
            .Where(x => x.DocumentId == documentId)
            .ToDictionaryAsync(x => x.LineId, x => x.ValueDelta, ct);

    public async Task<IReadOnlyDictionary<Guid, DeliveryCost>> GetDeliveryCostsAsync(
        IReadOnlyCollection<Guid> deliveryLineIds, CancellationToken ct)
    {
        var ids = deliveryLineIds.Distinct().ToList();
        var issued = await (
                from entry in db.Set<InventoryCostEntry>().AsNoTracking()
                join document in db.Set<StockDocument>() on entry.DocumentId equals document.Id
                where ids.Contains(entry.LineId) && document.DocumentType == StockDocumentType.SalesDelivery
                select new { entry.LineId, Quantity = -entry.QuantityDelta, Value = -entry.ValueDelta })
            .ToListAsync(ct);
        var returned = await (
                from line in db.Set<StockDocumentLine>().AsNoTracking()
                join document in db.Set<StockDocument>() on line.DocumentId equals document.Id
                join entry in db.Set<InventoryCostEntry>() on line.Id equals entry.LineId
                where line.ReversesLineId != null && ids.Contains(line.ReversesLineId.Value)
                      && document.DocumentType == StockDocumentType.SalesReturn
                group entry by line.ReversesLineId!.Value into g
                select new { LineId = g.Key, Quantity = g.Sum(e => e.QuantityDelta), Value = g.Sum(e => e.ValueDelta) })
            .ToDictionaryAsync(x => x.LineId, ct);
        return issued.ToDictionary(
            x => x.LineId,
            x => returned.TryGetValue(x.LineId, out var back)
                ? new DeliveryCost(x.Quantity, x.Value, back.Quantity, back.Value)
                : new DeliveryCost(x.Quantity, x.Value, 0m, 0m));
    }

    public void Add(InventoryCostState state) => db.Add(state);

    public void Add(InventoryCostEntry entry) => db.Add(entry);
}
