using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence;

// Balances and availability change only here, in the posting's transaction, with atomic conditional UPDATEs: a
// decrease matches no row when it would go below zero (or, for an ordinary issue, below the reserved quantity), and the
// posting stops with a shortage. Rows are taken in one fixed order (availability by warehouse then product, then
// balances by warehouse, location, product), so concurrent postings queue instead of deadlocking. Upserts hold a
// key-range lock (UPDLOCK, HOLDLOCK) so two postings can't both insert the same new row. Reservations raise and lower
// ReservedQuantity here too, with the same conditional updates, so a reservation and an issue can't both take the last unit.
internal sealed class StockLedger(DbContext db) : IStockLedger
{
    private const string AddAvailable = """
        UPDATE [inventory].[ProductWarehouseStock] WITH (UPDLOCK, HOLDLOCK)
        SET [OnHandSellable] = [OnHandSellable] + @quantity
        WHERE [ProductId] = @product AND [WarehouseId] = @warehouse;
        IF @@ROWCOUNT = 0
            INSERT INTO [inventory].[ProductWarehouseStock] ([ProductId], [WarehouseId], [OnHandSellable], [ReservedQuantity])
            VALUES (@product, @warehouse, @quantity, 0);
        """;

    // Ordinary issues take only unreserved stock.
    private const string TakeUnreserved = """
        UPDATE [inventory].[ProductWarehouseStock]
        SET [OnHandSellable] = [OnHandSellable] - @quantity
        OUTPUT inserted.[OnHandSellable], inserted.[ReservedQuantity]
        WHERE [ProductId] = @product AND [WarehouseId] = @warehouse AND [OnHandSellable] - [ReservedQuantity] >= @quantity;
        """;

    // Authorized decreases may cut into reserved stock, never below zero.
    private const string TakeOnHand = """
        UPDATE [inventory].[ProductWarehouseStock]
        SET [OnHandSellable] = [OnHandSellable] - @quantity
        OUTPUT inserted.[OnHandSellable], inserted.[ReservedQuantity]
        WHERE [ProductId] = @product AND [WarehouseId] = @warehouse AND [OnHandSellable] >= @quantity;
        """;

    // A sales delivery takes the stock its order reserved: on hand and reserved go down together, and the stock must be
    // physically there (an over-reserved warehouse may hold less than is reserved).
    private const string TakeReserved = """
        UPDATE [inventory].[ProductWarehouseStock]
        SET [OnHandSellable] = [OnHandSellable] - @quantity, [ReservedQuantity] = [ReservedQuantity] - @quantity
        OUTPUT inserted.[OnHandSellable], inserted.[ReservedQuantity]
        WHERE [ProductId] = @product AND [WarehouseId] = @warehouse AND [OnHandSellable] >= @quantity
          AND [ReservedQuantity] >= @quantity;
        """;

    // A reservation takes only stock that is on hand and not reserved by anyone else.
    private const string Reserve = """
        UPDATE [inventory].[ProductWarehouseStock]
        SET [ReservedQuantity] = [ReservedQuantity] + @quantity
        WHERE [ProductId] = @product AND [WarehouseId] = @warehouse AND [OnHandSellable] - [ReservedQuantity] >= @quantity;
        """;

    private const string Release = """
        UPDATE [inventory].[ProductWarehouseStock]
        SET [ReservedQuantity] = [ReservedQuantity] - @quantity
        WHERE [ProductId] = @product AND [WarehouseId] = @warehouse AND [ReservedQuantity] >= @quantity;
        """;

    private const string AddBalance = """
        UPDATE [inventory].[StockBalances] WITH (UPDLOCK, HOLDLOCK)
        SET [Quantity] = [Quantity] + @quantity, [UpdatedAtUtc] = @now
        WHERE [ProductId] = @product AND [WarehouseId] = @warehouse AND [LocationId] = @location
          AND [LotId] IS NULL AND [StockStatus] = @status;
        IF @@ROWCOUNT = 0
            INSERT INTO [inventory].[StockBalances] ([ProductId], [WarehouseId], [LocationId], [LotId], [StockStatus], [Quantity], [UpdatedAtUtc])
            VALUES (@product, @warehouse, @location, NULL, @status, @quantity, @now);
        """;

    private const string TakeBalance = """
        UPDATE [inventory].[StockBalances]
        SET [Quantity] = [Quantity] - @quantity, [UpdatedAtUtc] = @now
        WHERE [ProductId] = @product AND [WarehouseId] = @warehouse AND [LocationId] = @location
          AND [LotId] IS NULL AND [StockStatus] = @status AND [Quantity] >= @quantity;
        """;

    public async Task<StockLedgerOutcome> ApplyAsync(StockPosting posting, CancellationToken ct)
    {
        var overReserved = new List<OverReservedStock>();

        var availability = posting.Changes
            .Where(c => c.Sellable)
            .GroupBy(c => (c.WarehouseId, c.ProductId))
            .OrderBy(g => g.Key.WarehouseId).ThenBy(g => g.Key.ProductId);
        foreach (var group in availability)
        {
            var (warehouseId, productId) = group.Key;
            var delta = group.Sum(c => c.Quantity);
            if (delta > 0)
                await db.Database.ExecuteSqlRawAsync(AddAvailable, [Product(productId), Warehouse(warehouseId), Quantity(delta)], ct);
            else if (delta < 0)
            {
                var sql = posting.ConsumesReservations ? TakeReserved : posting.MayOverReserve ? TakeOnHand : TakeUnreserved;
                var after = await TakeAvailableAsync(sql, productId, warehouseId, -delta, ct);
                if (after is null)
                    return new StockLedgerOutcome(new StockShortage(productId, warehouseId, LocationId: null), []);
                if (after.Value.OnHand < after.Value.Reserved)
                {
                    overReserved.Add(new OverReservedStock(productId, warehouseId, after.Value.OnHand, after.Value.Reserved));
                    db.Add(OverReservationAlert.Record(productId, warehouseId, posting.DocumentId, after.Value.OnHand, after.Value.Reserved, posting.PostedAtUtc));
                }
            }
        }

        var balances = posting.Changes
            .GroupBy(c => (c.WarehouseId, c.LocationId, c.ProductId))
            .OrderBy(g => g.Key.WarehouseId).ThenBy(g => g.Key.LocationId).ThenBy(g => g.Key.ProductId);
        foreach (var group in balances)
        {
            var (warehouseId, locationId, productId) = group.Key;
            var delta = group.Sum(c => c.Quantity);
            if (delta == 0)
                continue;
            SqlParameter[] parameters =
            [
                Product(productId), Warehouse(warehouseId), Location(locationId), Status(StockStatus.Available),
                Quantity(Math.Abs(delta)), Now(posting.PostedAtUtc),
            ];
            if (delta > 0)
                await db.Database.ExecuteSqlRawAsync(AddBalance, parameters, ct);
            else if (await db.Database.ExecuteSqlRawAsync(TakeBalance, parameters, ct) == 0)
                return new StockLedgerOutcome(new StockShortage(productId, warehouseId, locationId), []);
        }

        foreach (var change in posting.Changes)
            db.Add(StockMovement.Record(
                posting.DocumentId, change.LineId, change.AllocationId, change.ProductId, change.WarehouseId, change.LocationId,
                lotId: null, StockStatus.Available, change.Quantity, posting.PostedAtUtc));
        return new StockLedgerOutcome(Shortage: null, overReserved);
    }

    public async Task<bool> TryReserveAsync(Guid productId, Guid warehouseId, decimal baseQuantity, CancellationToken ct) =>
        await db.Database.ExecuteSqlRawAsync(Reserve, [Product(productId), Warehouse(warehouseId), Quantity(baseQuantity)], ct) == 1;

    // The reservation rows say this much is reserved, so the warehouse's total must hold it; anything else is corruption.
    public async Task ReleaseReservedAsync(Guid productId, Guid warehouseId, decimal baseQuantity, CancellationToken ct)
    {
        if (await db.Database.ExecuteSqlRawAsync(Release, [Product(productId), Warehouse(warehouseId), Quantity(baseQuantity)], ct) != 1)
            throw new InvalidOperationException(
                $"Warehouse {warehouseId} reserves less of product {productId} than its reservations release ({baseQuantity}).");
    }

    // The row after the decrease, or null when the condition matched nothing.
    private async Task<(decimal OnHand, decimal Reserved)?> TakeAvailableAsync(
        string sql, Guid productId, Guid warehouseId, decimal quantity, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.Parameters.Add(Product(productId));
        command.Parameters.Add(Warehouse(warehouseId));
        command.Parameters.Add(Quantity(quantity));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? (reader.GetDecimal(0), reader.GetDecimal(1)) : null;
    }

    private static SqlParameter Product(Guid id) => new("@product", SqlDbType.UniqueIdentifier) { Value = id };

    private static SqlParameter Warehouse(Guid id) => new("@warehouse", SqlDbType.UniqueIdentifier) { Value = id };

    private static SqlParameter Location(Guid id) => new("@location", SqlDbType.UniqueIdentifier) { Value = id };

    private static SqlParameter Status(StockStatus status) => new("@status", SqlDbType.TinyInt) { Value = (byte)status };

    private static SqlParameter Quantity(decimal quantity) =>
        new("@quantity", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = quantity };

    private static SqlParameter Now(DateTime now) => new("@now", SqlDbType.DateTime2) { Scale = 3, Value = now };
}
