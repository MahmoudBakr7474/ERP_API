using System.Data;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Reconciliation;
using WarehouseERP.Inventory.Application.Contracts.Reconciliation;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Inventory.Infrastructure.ReadModels;

// Everything is read inside one SNAPSHOT transaction (the database allows snapshot isolation since Migration05), so a
// posting committing meanwhile can't show up as a false difference. Comparisons run in memory and only report.
internal sealed class ReconciliationReadStore(DbContext db, IWarehouseDirectory warehouses, ReferenceNames names) : IReconciliationReadStore
{
    private static readonly StockTransferStatus[] OpenStatuses =
        [StockTransferStatus.InTransit, StockTransferStatus.PartiallyReceived, StockTransferStatus.Closing];

    public async Task<ReconciliationReport> GetAsync(DateTime nowUtc, CancellationToken ct)
    {
        var snapshot = await ReadSnapshotAsync(ct);
        var warehouseIds = snapshot.Balances.Select(b => b.WarehouseId).Concat(snapshot.Availability.Select(a => a.WarehouseId))
            .Concat(snapshot.Transfers.SelectMany(t => new[] { t.SourceWarehouseId, t.DestinationWarehouseId, t.TransitWarehouseId }))
            .Distinct().ToList();
        var warehouseInfos = (await warehouses.FindManyAsync(warehouseIds, ct)).ToDictionary(w => w.Id);
        bool IsTransit(Guid warehouseId) => warehouseInfos.TryGetValue(warehouseId, out var w) && w.Type == WarehouseType.Transit;

        // Sellable balances of each physical warehouse against its OnHandSellable (a transit warehouse should have none).
        var sellable = snapshot.Balances
            .Where(b => !IsTransit(b.WarehouseId) && b.StockStatus == StockStatus.Available)
            .GroupBy(b => (b.ProductId, b.WarehouseId))
            .ToDictionary(g => g.Key, g => g.Sum(b => b.Quantity));
        var onHand = snapshot.Availability.ToDictionary(a => (a.ProductId, a.WarehouseId), a => a.OnHandSellable);
        var availabilityDifferences = sellable.Keys.Union(onHand.Keys)
            .Select(k => (Key: k, Balance: sellable.GetValueOrDefault(k), OnHand: onHand.GetValueOrDefault(k)))
            .Where(x => x.Balance != x.OnHand)
            .ToList();

        // The movements posted to each location against its balance.
        var balanceByPlace = snapshot.Balances
            .GroupBy(b => (b.ProductId, b.WarehouseId, b.LocationId))
            .ToDictionary(g => g.Key, g => g.Sum(b => b.Quantity));
        var movementsByPlace = snapshot.Movements.ToDictionary(m => (m.ProductId, m.WarehouseId, m.LocationId), m => m.Quantity);
        var ledgerDifferences = balanceByPlace.Keys.Union(movementsByPlace.Keys)
            .Select(k => (Key: k, Movements: movementsByPlace.GetValueOrDefault(k), Balance: balanceByPlace.GetValueOrDefault(k)))
            .Where(x => x.Movements != x.Balance)
            .ToList();

        // Company stock (transit included) against the valuation, and the valuation against its entries.
        var stockByProduct = snapshot.Balances.GroupBy(b => b.ProductId).ToDictionary(g => g.Key, g => g.Sum(b => b.Quantity));
        var states = snapshot.States.ToDictionary(s => s.ProductId);
        var entries = snapshot.Entries.ToDictionary(e => e.ProductId);
        var valuationDifferences = stockByProduct.Keys.Union(states.Keys).Union(entries.Keys)
            .Select(p => (
                ProductId: p,
                Stock: stockByProduct.GetValueOrDefault(p),
                ValuedQuantity: states.TryGetValue(p, out var s) ? s.TotalQuantity : 0m,
                ValuedValue: states.TryGetValue(p, out var v) ? v.TotalValue : 0m,
                EntriesQuantity: entries.TryGetValue(p, out var e) ? e.Quantity : 0m,
                EntriesValue: entries.TryGetValue(p, out var f) ? f.Value : 0m))
            .Where(x => x.Stock != x.ValuedQuantity || x.EntriesQuantity != x.ValuedQuantity || x.EntriesValue != x.ValuedValue
                        || (x.ValuedQuantity == 0 && x.ValuedValue != 0))
            .ToList();

        // What each dispatched transfer still has in transit against its transit location's balance.
        var transitBalances = snapshot.Balances
            .Where(b => IsTransit(b.WarehouseId))
            .GroupBy(b => (b.LocationId, b.ProductId))
            .ToDictionary(g => g.Key, g => g.Sum(b => b.Quantity));
        var transferDifferences = new List<TransferDifference>();
        foreach (var transfer in snapshot.Transfers.Where(t => t.TransitLocationId.HasValue))
        {
            var location = transfer.TransitLocationId!.Value;
            var outstanding = transfer.Lines.GroupBy(l => l.ProductId).ToDictionary(g => g.Key, g => g.Sum(l => l.OutstandingQuantity));
            var inTransit = transitBalances.Where(b => b.Key.LocationId == location).ToDictionary(b => b.Key.ProductId, b => b.Value);
            foreach (var productId in outstanding.Keys.Union(inTransit.Keys))
                if (outstanding.GetValueOrDefault(productId) != inTransit.GetValueOrDefault(productId))
                    transferDifferences.Add(new TransferDifference(
                        transfer.Id, transfer.Number, productId, "", outstanding.GetValueOrDefault(productId), inTransit.GetValueOrDefault(productId)));
        }

        var productIds = stockByProduct.Keys.Union(states.Keys).Union(availabilityDifferences.Select(a => a.Key.ProductId))
            .Union(ledgerDifferences.Select(l => l.Key.ProductId)).Union(transferDifferences.Select(t => t.ProductId)).ToList();
        var lookup = await names.LoadAsync(
            productIds: productIds,
            warehouseIds: warehouseIds,
            locationIds: ledgerDifferences.Select(l => l.Key.LocationId),
            ct: ct);

        var positions = productIds
            .Select(p => new StockPosition(
                p, lookup.Sku(p), lookup.ProductName(p),
                snapshot.Balances.Where(b => b.ProductId == p && !IsTransit(b.WarehouseId)).Sum(b => b.Quantity),
                snapshot.Balances.Where(b => b.ProductId == p && IsTransit(b.WarehouseId)).Sum(b => b.Quantity),
                states.TryGetValue(p, out var s) ? s.TotalQuantity : 0m,
                states.TryGetValue(p, out var v) ? v.TotalValue : 0m))
            .Where(x => x.PhysicalQuantity != 0 || x.InTransitQuantity != 0 || x.ValuedQuantity != 0 || x.ValuedValue != 0)
            .OrderBy(x => x.Sku, StringComparer.Ordinal)
            .ToList();
        var openTransfers = snapshot.Transfers
            .Where(t => OpenStatuses.Contains(t.Status))
            .OrderBy(t => t.DispatchedAtUtc)
            .Select(t => new OpenTransferPosition(
                t.Id, t.Number, t.Status, lookup.WarehouseCode(t.SourceWarehouseId), lookup.WarehouseCode(t.DestinationWarehouseId),
                t.DispatchedAtUtc, t.DispatchedAtUtc is { } dispatched ? (int)(nowUtc - dispatched).TotalDays : 0,
                t.OutstandingQuantity))
            .ToList();

        var availability = availabilityDifferences
            .Select(x => new AvailabilityDifference(
                x.Key.ProductId, lookup.Sku(x.Key.ProductId), x.Key.WarehouseId, lookup.WarehouseCode(x.Key.WarehouseId), x.Balance, x.OnHand))
            .ToList();
        var ledger = ledgerDifferences
            .Select(x => new LedgerDifference(
                x.Key.ProductId, lookup.Sku(x.Key.ProductId), x.Key.WarehouseId, lookup.WarehouseCode(x.Key.WarehouseId),
                x.Key.LocationId, lookup.LocationCode(x.Key.LocationId), x.Movements, x.Balance))
            .ToList();
        var valuation = valuationDifferences
            .Select(x => new ValuationDifference(
                x.ProductId, lookup.Sku(x.ProductId), x.Stock, x.ValuedQuantity, x.EntriesQuantity, x.ValuedValue, x.EntriesValue))
            .ToList();
        var transfersOut = transferDifferences.Select(t => t with { Sku = lookup.Sku(t.ProductId) }).ToList();

        return new ReconciliationReport(
            nowUtc, availability.Count == 0 && ledger.Count == 0 && valuation.Count == 0 && transfersOut.Count == 0,
            availability, ledger, valuation, transfersOut, positions, openTransfers);
    }

    private async Task<Snapshot> ReadSnapshotAsync(CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Snapshot, ct);
        var balances = await db.Set<StockBalance>().AsNoTracking()
            .Where(b => b.Quantity != 0)
            .Select(b => new BalanceRow(b.ProductId, b.WarehouseId, b.LocationId, b.StockStatus, b.Quantity))
            .ToListAsync(ct);
        var availability = await db.Set<ProductWarehouseStock>().AsNoTracking()
            .Select(a => new AvailabilityRow(a.ProductId, a.WarehouseId, a.OnHandSellable))
            .ToListAsync(ct);
        var movements = await db.Set<StockMovement>().AsNoTracking()
            .GroupBy(m => new { m.ProductId, m.WarehouseId, m.LocationId })
            .Select(g => new MovementRow(g.Key.ProductId, g.Key.WarehouseId, g.Key.LocationId, g.Sum(m => m.Quantity)))
            .ToListAsync(ct);
        var states = await db.Set<InventoryCostState>().AsNoTracking().ToListAsync(ct);
        var entries = await db.Set<InventoryCostEntry>().AsNoTracking()
            .GroupBy(e => e.ProductId)
            .Select(g => new EntryRow(g.Key, g.Sum(e => e.QuantityDelta), g.Sum(e => e.ValueDelta)))
            .ToListAsync(ct);
        var transitLocations = balances.Select(b => b.LocationId).Distinct().ToArray();
        var transfers = await db.Set<StockTransfer>().AsNoTracking()
            .Include(t => t.Lines)
            .Where(t => OpenStatuses.Contains(t.Status)
                        || (t.TransitLocationId != null && transitLocations.Contains(t.TransitLocationId.Value)))
            .ToListAsync(ct);
        await transaction.CommitAsync(ct);
        return new Snapshot(balances, availability, movements, states, entries, transfers);
    }

    private sealed record BalanceRow(Guid ProductId, Guid WarehouseId, Guid LocationId, StockStatus StockStatus, decimal Quantity);

    private sealed record AvailabilityRow(Guid ProductId, Guid WarehouseId, decimal OnHandSellable);

    private sealed record MovementRow(Guid ProductId, Guid WarehouseId, Guid LocationId, decimal Quantity);

    private sealed record EntryRow(Guid ProductId, decimal Quantity, decimal Value);

    private sealed record Snapshot(
        List<BalanceRow> Balances, List<AvailabilityRow> Availability, List<MovementRow> Movements,
        List<InventoryCostState> States, List<EntryRow> Entries, List<StockTransfer> Transfers);
}
