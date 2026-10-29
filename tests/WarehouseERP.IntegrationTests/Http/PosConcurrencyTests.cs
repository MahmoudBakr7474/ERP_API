using System.Net;
using static WarehouseERP.IntegrationTests.Http.PosTesting;
using Stockroom = WarehouseERP.IntegrationTests.Http.InventoryTesting.Stockroom;

namespace WarehouseERP.IntegrationTests.Http;

// What POS V1 must hold under concurrency on SQL Server: tills racing for the last units sell them once, a till never
// sells what a sales order reserved even racing its confirmation, a checkout retried while its first attempt runs replays
// it instead of selling twice, concurrent returns of one sale line never take back more than it sold, and a refused
// checkout leaves no sale, no stock movement, no payment and no receipt number behind.
[Collection(DatabaseCollection.Name)]
public sealed class PosConcurrencyTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Client(Guid user) => _api.CreateClient().As(AllPermissions, userId: user);

    private static async Task<Guid> AddTerminalAsync(HttpClient admin, Stockroom room) =>
        await admin.PostJsonAsync(Terminals, new
        {
            code = DatabaseFixture.Code("T")[..12].ToUpperInvariant(), name = "كاشير", warehouseId = room.WarehouseId,
            sellingLocationId = (Guid?)null, acceptsCash = true, acceptsCard = true,
        }).CreatedIdAsync();

    [Fact]
    public async Task Tills_racing_for_the_last_units_sell_them_only_once()
    {
        var admin = Client(Guid.NewGuid());
        var till = await CreateTillAsync(fx, admin, stock: 5m);
        var cashiers = new List<(HttpClient Client, Guid TerminalId)>();
        for (var i = 0; i < 4; i++)
        {
            var cashier = Client(Guid.NewGuid());
            var terminalId = i == 0 ? till.TerminalId : await AddTerminalAsync(admin, till.Room);
            await OpenShiftAsync(cashier, terminalId).CreatedIdAsync();
            cashiers.Add((cashier, terminalId));
        }

        var responses = await Task.WhenAll(cashiers.Select(c =>
            CheckoutAsync(c.Client, c.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 2m)), Pay(Cash(22.80m)))));

        Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        foreach (var refused in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
            await refused.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
        Assert.Equal(1m, await InventoryTesting.BalanceAsync(admin, till.Room));
        Assert.Equal(2, (await admin.GetJsonAsync($"{SalesRoute}?warehouseId={till.Room.WarehouseId}")).GetProperty("totalCount").GetInt32());
        await InventoryTesting.AssertValuedAsync(admin, till.Room.ProductId, 1m, 6m);
        await AssertReconciledAsync(admin, till.Room);
    }

    [Fact]
    public async Task A_till_never_sells_stock_a_sales_order_reserved_even_racing_its_confirmation()
    {
        var admin = Client(Guid.NewGuid());
        var till = await CreateTillAsync(fx, admin, stock: 5m);
        await OpenShiftAsync(admin, till.TerminalId).CreatedIdAsync();
        var customerId = await SalesTesting.CreateCustomerAsync(admin);
        await SalesTesting.ConfirmedOrderAsync(admin, customerId, till.Room.WarehouseId, SalesTesting.Line(till.Room.ProductId, till.Room.Piece, 3m));

        await (await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 3m)), Pay(Cash(34.20m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
        Assert.Equal((5m, 3m), await SalesTesting.AvailabilityAsync(admin, till.Room.WarehouseId, till.Room.ProductId));

        var orderId = await SalesTesting.DraftOrderAsync(admin, customerId, till.Room.WarehouseId,
            SalesTesting.Line(till.Room.ProductId, till.Room.Piece, 2m)).CreatedIdAsync();
        var orderVersion = (await SalesTesting.GetAsync(admin, SalesTesting.Orders, orderId)).Text("rowVersion");

        var responses = await Task.WhenAll(
            SalesTesting.ActAsync(Client(Guid.NewGuid()), SalesTesting.Orders, orderId, "submit", rowVersion: orderVersion),
            CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 2m)), Pay(Cash(22.80m))));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        await responses.Single(r => r.StatusCode != HttpStatusCode.OK)
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
        var (onHand, reserved) = await SalesTesting.AvailabilityAsync(admin, till.Room.WarehouseId, till.Room.ProductId);
        Assert.True((onHand, reserved) is (5m, 5m) or (3m, 3m), $"on hand {onHand}, reserved {reserved}");
        await AssertReconciledAsync(admin, till.Room);
    }

    [Fact]
    public async Task A_checkout_retried_while_its_first_attempt_runs_is_recorded_once()
    {
        var admin = Client(Guid.NewGuid());
        var till = await CreateTillAsync(fx, admin, stock: 10m);
        var shiftId = await OpenShiftAsync(admin, till.TerminalId).CreatedIdAsync();
        var key = Guid.NewGuid();

        var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ =>
            CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 2m)), Pay(Cash(30m)), key)));

        var sold = responses.Where(r => r.StatusCode == HttpStatusCode.OK).ToList();
        foreach (var busy in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
            await busy.AssertProblemAsync(HttpStatusCode.Conflict, "Pos.Idempotency.Busy");
        Assert.NotEmpty(sold);
        var ids = new HashSet<Guid>();
        foreach (var response in sold)
            ids.Add((await Task.FromResult(response).SoldAsync()).GetProperty("id").GetGuid());
        Assert.Single(ids);
        Assert.Equal(8m, await InventoryTesting.BalanceAsync(admin, till.Room));
        var shift = await ShiftAsync(admin, shiftId);
        Assert.Equal((1, 22.80m), (shift.GetProperty("salesCount").GetInt32(), shift.Decimal("cashSalesAmount")));
    }

    [Fact]
    public async Task Concurrent_returns_of_one_sale_line_never_take_back_more_than_it_sold()
    {
        var admin = Client(Guid.NewGuid());
        var till = await CreateTillAsync(fx, admin, stock: 10m);
        await OpenShiftAsync(admin, till.TerminalId).CreatedIdAsync();
        var sale = await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 3m)), Pay(Cash(34.20m))).SoldAsync();
        var saleId = sale.GetProperty("id").GetGuid();
        var lineId = sale.GetProperty("lines")[0].GetProperty("id").GetGuid();

        var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ =>
            ReturnAsync(admin, till.TerminalId, saleId, Lines(ReturnLine(lineId, 2m)), Pay(Cash(22.80m)))));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        foreach (var refused in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
            await refused.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Return.ExceedsSale");
        var after = await admin.GetJsonAsync($"{SalesRoute}/{saleId}");
        Assert.Equal((2m, 22.80m), (after.GetProperty("lines")[0].Decimal("returnedQuantity"), after.Decimal("refundedAmount")));
        Assert.Equal(9m, await InventoryTesting.BalanceAsync(admin, till.Room));
        await AssertReconciledAsync(admin, till.Room);
    }

    [Fact]
    public async Task A_refused_checkout_leaves_no_sale_stock_movement_payment_or_receipt_number_behind()
    {
        var admin = Client(Guid.NewGuid());
        var till = await CreateTillAsync(fx, admin, stock: 3m);
        var shiftId = await OpenShiftAsync(admin, till.TerminalId).CreatedIdAsync();

        var first = await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m))).SoldAsync();
        await (await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 5m)), Pay(Cash(57m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
        var shift = await ShiftAsync(admin, shiftId);
        Assert.Equal((1, 11.40m, 111.40m), (shift.GetProperty("salesCount").GetInt32(), shift.Decimal("cashSalesAmount"), shift.Decimal("expectedCash")));
        Assert.Equal(2m, await InventoryTesting.BalanceAsync(admin, till.Room));

        var next = await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m))).SoldAsync();
        Assert.Equal(($"{till.TerminalCode}-2026-000001", $"{till.TerminalCode}-2026-000002"), (first.Text("number"), next.Text("number")));
        await AssertReconciledAsync(admin, till.Room);
    }

    private static async Task AssertReconciledAsync(HttpClient admin, Stockroom room)
    {
        var report = await admin.GetJsonAsync("/api/inventory/reconciliation");
        foreach (var section in new[] { "availabilityDifferences", "ledgerDifferences", "valuationDifferences", "reservationDifferences" })
            Assert.DoesNotContain(report.GetProperty(section).EnumerateArray(), d => d.GetProperty("productId").GetGuid() == room.ProductId);
    }
}
