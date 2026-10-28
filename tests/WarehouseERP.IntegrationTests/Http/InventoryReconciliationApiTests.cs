using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Persistence;
using static WarehouseERP.IntegrationTests.Http.InventoryTesting;

namespace WarehouseERP.IntegrationTests.Http;

// The reconciliation report through the real host on SQL Server. The database is shared, so each test looks only at
// its own products: stock in transit belongs to the company's valuation, and differences are reported, never corrected.
[Collection(DatabaseCollection.Name)]
public sealed class InventoryReconciliationApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const string Reconciliation = "/api/inventory/reconciliation";

    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(AllPermissions);

    private static IEnumerable<JsonElement> About(JsonElement report, string list, Guid productId) =>
        report.GetProperty(list).EnumerateArray().Where(d => d.GetProperty("productId").GetGuid() == productId);

    [Fact]
    public async Task Physical_stock_plus_stock_in_transit_is_what_the_company_values()
    {
        var admin = Admin();
        var route = await CreateRouteAsync(fx, admin);
        var room = route.Source;
        await ReceiveAsync(admin, room, 20m, 5m);
        var (id, lineId) = await DispatchPiecesAsync(admin, route, 12m);
        await ReceiveTransferAsync(_api.CreateClient().As(AllPermissions, userId: Approver), id, null, null, (lineId, 5m)).PostedAsync();

        var report = await admin.GetJsonAsync(Reconciliation);

        var position = Assert.Single(About(report, "stockPositions", room.ProductId));
        Assert.Equal((13m, 7m, 20m, 100m), (position.GetProperty("physicalQuantity").GetDecimal(),
            position.GetProperty("inTransitQuantity").GetDecimal(), position.GetProperty("valuedQuantity").GetDecimal(),
            position.GetProperty("valuedValue").GetDecimal()));
        var open = Assert.Single(report.GetProperty("openTransfers").EnumerateArray(), t => t.GetProperty("transferId").GetGuid() == id);
        Assert.Equal(("PartiallyReceived", 7m, 0), (open.GetProperty("status").GetString(),
            open.GetProperty("outstandingQuantity").GetDecimal(), open.GetProperty("daysInTransit").GetInt32()));
        foreach (var list in new[] { "availabilityDifferences", "ledgerDifferences", "valuationDifferences", "transferDifferences" })
            Assert.Empty(About(report, list, room.ProductId));
    }

    [Fact]
    public async Task Differences_are_reported_and_nothing_is_corrected()
    {
        var admin = Admin();
        var room = await CreateStockroomAsync(fx, admin);
        await ReceiveAsync(admin, room, 6m, 2m);
        await ShiftAsync(room, balance: +2m, onHand: +1m);
        try
        {
            var report = await admin.GetJsonAsync(Reconciliation);

            Assert.False(report.GetProperty("isConsistent").GetBoolean());
            var availability = Assert.Single(About(report, "availabilityDifferences", room.ProductId));
            Assert.Equal((8m, 7m), (availability.GetProperty("sellableBalance").GetDecimal(), availability.GetProperty("onHandSellable").GetDecimal()));
            var ledger = Assert.Single(About(report, "ledgerDifferences", room.ProductId));
            Assert.Equal((6m, 8m, "DEFAULT"), (ledger.GetProperty("movementsQuantity").GetDecimal(),
                ledger.GetProperty("balanceQuantity").GetDecimal(), ledger.GetProperty("locationCode").GetString()));
            var valuation = Assert.Single(About(report, "valuationDifferences", room.ProductId));
            Assert.Equal((8m, 6m, 6m), (valuation.GetProperty("stockQuantity").GetDecimal(),
                valuation.GetProperty("valuedQuantity").GetDecimal(), valuation.GetProperty("entriesQuantity").GetDecimal()));

            Assert.Equal(8m, await BalanceAsync(admin, room)); // reported, not corrected
            var again = await admin.GetJsonAsync(Reconciliation);
            Assert.Single(About(again, "ledgerDifferences", room.ProductId));
        }
        finally
        {
            await ShiftAsync(room, balance: -2m, onHand: -1m);
        }

        var repaired = await admin.GetJsonAsync(Reconciliation);
        foreach (var list in new[] { "availabilityDifferences", "ledgerDifferences", "valuationDifferences" })
            Assert.Empty(About(repaired, list, room.ProductId));
    }

    [Fact]
    public async Task Only_a_company_wide_user_with_the_permission_reads_the_report()
    {
        var admin = Admin();
        var room = await CreateStockroomAsync(fx, admin);

        await (await _api.CreateClient().As(InventoryPermissions.ReconciliationRead, room.WarehouseId.ToString()).GetAsync(Reconciliation))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.Reconciliation.CompanyWideAccessRequired");
        await (await _api.CreateClient().As(InventoryPermissions.DocumentsRead).GetAsync(Reconciliation))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
    }

    [Fact]
    public async Task The_report_hands_its_connection_back_at_read_committed()
    {
        // A connection pool of its own, so the next query gets the connection the report used.
        await using var api = new ApiFactory(fx.ConnectionString + ";Application Name=ReconciliationIsolation", clock: fx.Clock);
        (await api.CreateClient().As(AllPermissions).GetAsync(Reconciliation)).EnsureSuccessStatusCode();

        await using var scope = api.Services.CreateAsyncScope();
        var level = await scope.ServiceProvider.GetRequiredService<ErpDbContext>().Database
            .SqlQueryRaw<short>("SELECT [transaction_isolation_level] AS [Value] FROM sys.dm_exec_sessions WHERE [session_id] = @@SPID")
            .SingleAsync();
        Assert.Equal(2, level); // READ COMMITTED; 5 (SNAPSHOT) made the next request's updates fail with update conflicts
    }

    // Changes stored totals the way only a bug or a hand-made SQL update could.
    private async Task ShiftAsync(Stockroom room, decimal balance, decimal onHand)
    {
        await using var db = fx.NewDb();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [inventory].[StockBalances] SET [Quantity] = [Quantity] + {balance} WHERE [WarehouseId] = {room.WarehouseId} AND [ProductId] = {room.ProductId}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [inventory].[ProductWarehouseStock] SET [OnHandSellable] = [OnHandSellable] + {onHand} WHERE [WarehouseId] = {room.WarehouseId} AND [ProductId] = {room.ProductId}");
    }
}
