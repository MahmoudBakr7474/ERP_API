using Microsoft.EntityFrameworkCore;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
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

    public void Add(InventoryCostState state) => db.Add(state);

    public void Add(InventoryCostEntry entry) => db.Add(entry);
}
