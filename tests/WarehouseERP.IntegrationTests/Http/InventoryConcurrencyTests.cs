using System.Net;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using static WarehouseERP.IntegrationTests.Http.InventoryTesting;

namespace WarehouseERP.IntegrationTests.Http;

// What INVENTORY_DECISIONS.md requires before production use, run against SQL Server: concurrent postings never sell
// stock twice, quantities and the moving average reconcile, and a warehouse or location holding stock can't be retired,
// not even by a deactivation racing a posting.
[Collection(DatabaseCollection.Name)]
public sealed class InventoryConcurrencyTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(AllPermissions);

    [Fact]
    public async Task Concurrent_issues_of_the_last_units_post_only_what_exists()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        await ReceiveAsync(client, room, 5m, 1m);
        var drafts = new List<(Guid Id, string RowVersion)>();
        for (var i = 0; i < 4; i++)
        {
            var id = await DraftIdAsync(client, StockDocumentType.Issue, room.WarehouseId, null, Line(room.ProductId, room.Piece, 2m));
            drafts.Add((id, (await DocumentAsync(client, id)).GetProperty("rowVersion").GetString()!));
        }

        var responses = await Task.WhenAll(drafts.Select(d => ActAsync(Admin(), d.Id, "post", rowVersion: d.RowVersion)));

        Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        foreach (var refused in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
            await refused.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
        Assert.Equal(1m, await BalanceAsync(client, room));
        await AssertReconciledAsync(room);
    }

    [Fact]
    public async Task Concurrent_receipts_and_issues_reconcile_quantities_and_the_moving_average()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        await ReceiveAsync(client, room, 50m, 4m);
        var drafts = new List<(Guid Id, string RowVersion)>();
        for (var i = 0; i < 6; i++)
        {
            var id = i % 2 == 0
                ? await DraftIdAsync(client, StockDocumentType.Receipt, room.WarehouseId, null, Line(room.ProductId, room.Piece, 3m + i, 4m + i))
                : await DraftIdAsync(client, StockDocumentType.Issue, room.WarehouseId, null, Line(room.ProductId, room.Piece, 5m));
            drafts.Add((id, (await DocumentAsync(client, id)).GetProperty("rowVersion").GetString()!));
        }

        var responses = await Task.WhenAll(drafts.Select(d => ActAsync(Admin(), d.Id, "post", rowVersion: d.RowVersion)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(50m + 3m + 5m + 7m - 15m, await BalanceAsync(client, room));
        await AssertReconciledAsync(room);
        var numbers = new List<string>();
        foreach (var draft in drafts)
            numbers.Add((await DocumentAsync(client, draft.Id)).GetProperty("number").GetString()!);
        Assert.Equal(6, numbers.Distinct().Count());
    }

    [Fact]
    public async Task A_warehouse_or_location_holding_stock_or_awaiting_approval_cannot_be_deactivated()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        var shelf = await client.PostJsonAsync($"{Warehouses}/{room.WarehouseId}/locations", new { code = "SHELF-1", nameAr = "رف" }).CreatedIdAsync();
        await ReceiveAsync(client, room, 3m, 1m, shelf);

        await (await client.PostAsync($"{Warehouses}/{room.WarehouseId}/locations/{shelf}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.WarehouseLocation.HoldsStock");
        await (await client.PostAsync($"{Warehouses}/{room.WarehouseId}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Warehouse.HoldsStock");

        await IssueAsync(client, room, 3m, shelf).PostedAsync();
        var pending = await DraftIdAsync(client, StockDocumentType.Adjustment, room.WarehouseId, AdjustmentReason.Found,
            Line(room.ProductId, room.Piece, 1m, 1m, StockDirection.In, shelf));
        Assert.Equal(HttpStatusCode.OK, (await ActAsync(client, pending, "submit")).StatusCode);
        await (await client.PostAsync($"{Warehouses}/{room.WarehouseId}/locations/{shelf}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.WarehouseLocation.HasOpenInventoryDocuments");
        await (await client.PostAsync($"{Warehouses}/{room.WarehouseId}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Warehouse.HasOpenInventoryDocuments");
        Assert.Equal(HttpStatusCode.OK, (await ActAsync(client, pending, "cancel")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Warehouses}/{room.WarehouseId}/locations/{shelf}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Warehouses}/{room.WarehouseId}/deactivate", null)).StatusCode);
        await (await DraftAsync(client, StockDocumentType.Receipt, room.WarehouseId, null, Line(room.ProductId, room.Piece, 1m, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.WarehouseNotActive");
    }

    [Fact]
    public async Task A_deactivation_racing_a_receipt_never_leaves_stock_in_an_inactive_warehouse()
    {
        for (var round = 0; round < 3; round++)
        {
            var client = Admin();
            var room = await CreateStockroomAsync(fx, client);
            var id = await DraftIdAsync(client, StockDocumentType.Receipt, room.WarehouseId, null, Line(room.ProductId, room.Piece, 2m, 1m));
            var rowVersion = (await DocumentAsync(client, id)).GetProperty("rowVersion").GetString();

            var responses = await Task.WhenAll(
                ActAsync(Admin(), id, "post", rowVersion: rowVersion),
                Admin().PostAsync($"{Warehouses}/{room.WarehouseId}/deactivate", null));
            var (posting, deactivation) = (responses[0], responses[1]);

            var posted = posting.StatusCode == HttpStatusCode.OK;
            var deactivated = deactivation.StatusCode == HttpStatusCode.NoContent;
            Assert.True(posted != deactivated, $"posted {posting.StatusCode}, deactivation {deactivation.StatusCode}");
            if (posted)
                await deactivation.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Warehouse.HoldsStock");
            else
                await posting.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.WarehouseNotActive");
        }
    }

    // Quantity ledger, balances, availability and valuation tell the same story.
    private async Task AssertReconciledAsync(Stockroom room)
    {
        await using var db = fx.NewDb();
        var ledger = await db.Set<StockMovement>().Where(m => m.ProductId == room.ProductId).SumAsync(m => m.Quantity);
        var balances = await db.Set<StockBalance>().Where(b => b.ProductId == room.ProductId).SumAsync(b => b.Quantity);
        var available = await db.Set<ProductWarehouseStock>().Where(s => s.ProductId == room.ProductId).SumAsync(s => s.OnHandSellable);
        var state = await db.Set<InventoryCostState>().SingleAsync(s => s.ProductId == room.ProductId);
        var entries = await db.Set<InventoryCostEntry>().Where(e => e.ProductId == room.ProductId).ToListAsync();

        Assert.Equal(ledger, balances);
        Assert.Equal(ledger, available);
        Assert.Equal(ledger, state.TotalQuantity);
        Assert.Equal(entries.Sum(e => e.QuantityDelta), state.TotalQuantity);
        Assert.Equal(entries.Sum(e => e.ValueDelta), state.TotalValue);
        Assert.True(state.TotalValue >= 0 && state.AverageUnitCost >= 0);
    }
}
