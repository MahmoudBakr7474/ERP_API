using System.Net;
using System.Text.Json;
using WarehouseERP.Inventory.Contracts;
using static WarehouseERP.IntegrationTests.Http.SalesTesting;
using Stockroom = WarehouseERP.IntegrationTests.Http.InventoryTesting.Stockroom;

namespace WarehouseERP.IntegrationTests.Http;

// What Sales V1 must hold under concurrency on SQL Server: the last units are reserved once, a reservation is never sold by
// an ordinary issue, deliveries and invoices never exceed the order however many post at once, a repeated posting replays
// instead of posting twice, a refused posting changes nothing, and reducing, closing or cancelling an order gives its
// reservation back without touching other orders'.
[Collection(DatabaseCollection.Name)]
public sealed class SalesConcurrencyTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(AllPermissions);

    private async Task<(Stockroom Room, Guid CustomerId)> SetUpAsync(HttpClient admin, decimal stock)
    {
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        await InventoryTesting.ReceiveAsync(admin, room, stock, 6m);
        await SetPriceAsync(admin, room.Piece, 10m);
        return (room, await CreateCustomerAsync(admin));
    }

    [Fact]
    public async Task Concurrent_confirmations_reserve_the_last_units_only_once()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin, stock: 5m);
        var drafts = new List<(Guid Id, string RowVersion)>();
        for (var i = 0; i < 4; i++)
        {
            var id = await DraftOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 2m)).CreatedIdAsync();
            drafts.Add((id, (await GetAsync(admin, Orders, id)).Text("rowVersion")!));
        }

        var responses = await Task.WhenAll(drafts.Select(d => ActAsync(Admin(), Orders, d.Id, "submit", rowVersion: d.RowVersion)));

        Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        foreach (var refused in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
            await refused.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
        var statuses = new List<string?>();
        foreach (var draft in drafts)
            statuses.Add((await GetAsync(admin, Orders, draft.Id)).Text("status"));
        Assert.Equal(2, statuses.Count(s => s == "Confirmed"));
        Assert.Equal(2, statuses.Count(s => s == "Draft")); // a refused confirmation leaves a draft to retry, unnumbered
        Assert.Equal((5m, 4m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));
        await AssertReconciledAsync(admin, room);
    }

    [Fact]
    public async Task An_ordinary_issue_takes_only_unreserved_stock_even_racing_a_confirmation()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin, stock: 5m);
        await ConfirmedOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 3m));

        await (await InventoryTesting.IssueAsync(admin, room, 3m)).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
        Assert.Equal((5m, 3m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));

        var orderId = await DraftOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 2m)).CreatedIdAsync();
        var orderVersion = (await GetAsync(admin, Orders, orderId)).Text("rowVersion");
        var issueId = await InventoryTesting.DraftIdAsync(admin, StockDocumentType.Issue, room.WarehouseId, null,
            InventoryTesting.Line(room.ProductId, room.Piece, 2m));
        var issueVersion = (await InventoryTesting.DocumentAsync(admin, issueId)).GetProperty("rowVersion").GetString();

        var responses = await Task.WhenAll(
            ActAsync(Admin(), Orders, orderId, "submit", rowVersion: orderVersion),
            InventoryTesting.ActAsync(Admin(), issueId, "post", rowVersion: issueVersion));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        await responses.Single(r => r.StatusCode != HttpStatusCode.OK)
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
        var (onHand, reserved) = await AvailabilityAsync(admin, room.WarehouseId, room.ProductId);
        Assert.True((onHand, reserved) is (5m, 5m) or (3m, 3m), $"on hand {onHand}, reserved {reserved}");
        await AssertReconciledAsync(admin, room);
    }

    [Fact]
    public async Task A_delivery_picks_from_several_locations_but_never_more_than_a_location_holds_or_the_order_wants()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin, stock: 6m);
        var shelf = await InventoryTesting.AddLocationAsync(admin, room.WarehouseId);
        await InventoryTesting.ReceiveAsync(admin, room, 4m, 6m, shelf);
        var (orderId, lines) = await ConfirmedOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 8m));

        await (await DraftDeliveryAsync(admin, orderId, (lines[0], 5m, room.DefaultLocationId), (lines[0], 4m, shelf)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Order.OverDelivery");
        await (await DraftDeliveryAsync(admin, orderId, (lines[0], 1m, shelf), (lines[0], 1m, shelf)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Delivery.DuplicatePick");

        // More than the shelf holds: refused at posting, and the refusal leaves the draft, the order and the stock untouched.
        var tooMuchHere = await DraftDeliveryAsync(admin, orderId, (lines[0], 5m, shelf)).CreatedIdAsync();
        await (await ActAsync(admin, Deliveries, tooMuchHere, "post", idempotencyKey: Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.InsufficientAtLocation");
        var refused = await GetAsync(admin, Deliveries, tooMuchHere);
        Assert.Equal(("Draft", JsonValueKind.Null), (refused.Text("status"), refused.GetProperty("number").ValueKind));
        Assert.Equal((10m, 8m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));
        await ActAsync(admin, Deliveries, tooMuchHere, "cancel").EnsureOkAsync();

        var first = await DeliverAsync(admin, orderId, (lines[0], 3m, room.DefaultLocationId), (lines[0], 3m, shelf));
        var delivery = await GetAsync(admin, Deliveries, first);
        Assert.Equal(36m, delivery.Decimal("costAmount"));
        Assert.Equal((3m, 1m), (await InventoryTesting.BalanceAsync(admin, room, room.DefaultLocationId), await InventoryTesting.BalanceAsync(admin, room, shelf)));
        Assert.Equal("PartiallyDelivered", (await GetAsync(admin, Orders, orderId)).Text("status"));
        Assert.Equal((4m, 2m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));

        await DeliverAsync(admin, orderId, (lines[0], 2m, null)); // no location: the warehouse's default
        var order = await GetAsync(admin, Orders, orderId);
        Assert.Equal(("Delivered", 8m, 0m), (order.Text("status"), order.GetProperty("lines")[0].Decimal("deliveredQuantity"),
            order.GetProperty("lines")[0].Decimal("remainingQuantity")));
        Assert.Equal((2m, 0m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));
        await AssertReconciledAsync(admin, room);
    }

    [Fact]
    public async Task Concurrent_deliveries_of_one_order_never_deliver_more_than_it_ordered()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin, stock: 10m);
        var (orderId, lines) = await ConfirmedOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 5m));
        var drafts = new List<(Guid Id, string RowVersion)>();
        for (var i = 0; i < 4; i++)
        {
            var id = await DraftDeliveryAsync(admin, orderId, (lines[0], 2m, null)).CreatedIdAsync();
            drafts.Add((id, (await GetAsync(admin, Deliveries, id)).Text("rowVersion")!));
        }

        var responses = await Task.WhenAll(drafts.Select(d =>
            ActAsync(Admin(), Deliveries, d.Id, "post", idempotencyKey: Guid.NewGuid(), rowVersion: d.RowVersion)));

        Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        foreach (var refused in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
            await refused.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Order.OverDelivery");
        var line = (await GetAsync(admin, Orders, orderId)).GetProperty("lines")[0];
        Assert.Equal((4m, 1m), (line.Decimal("deliveredQuantity"), line.Decimal("remainingQuantity")));
        Assert.Equal((6m, 1m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));
        await AssertReconciledAsync(admin, room);
    }

    [Fact]
    public async Task Concurrent_invoices_of_one_order_never_invoice_more_than_was_delivered()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin, stock: 10m);
        var (orderId, lines) = await ConfirmedOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 4m));
        await DeliverAsync(admin, orderId, (lines[0], 3m, null));
        var drafts = new List<(Guid Id, string RowVersion)>();
        for (var i = 0; i < 3; i++)
        {
            var id = await DraftInvoiceAsync(admin, orderId, (lines[0], 2m)).CreatedIdAsync();
            drafts.Add((id, (await GetAsync(admin, Invoices, id)).Text("rowVersion")!));
        }

        var responses = await Task.WhenAll(drafts.Select(d =>
            ActAsync(Admin(), Invoices, d.Id, "post", idempotencyKey: Guid.NewGuid(), rowVersion: d.RowVersion)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        foreach (var refused in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
            await refused.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Order.InvoiceExceedsDeliveries");
        var order = await GetAsync(admin, Orders, orderId);
        Assert.Equal((2m, 1m, 22.8m), (order.GetProperty("lines")[0].Decimal("invoicedQuantity"),
            order.GetProperty("lines")[0].Decimal("invoiceableQuantity"), order.Decimal("invoicedAmount")));
    }

    [Fact]
    public async Task Posting_a_delivery_again_with_its_key_replays_it_and_another_request_cannot_reuse_the_key()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin, stock: 10m);
        var (orderId, lines) = await ConfirmedOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 4m));
        var deliveryId = await DraftDeliveryAsync(admin, orderId, (lines[0], 2m, null)).CreatedIdAsync();
        var rowVersion = (await GetAsync(admin, Deliveries, deliveryId)).Text("rowVersion");
        var key = Guid.NewGuid();

        var first = await ActAsync(admin, Deliveries, deliveryId, "post", idempotencyKey: key, rowVersion: rowVersion).PostedAsync();
        var replay = await ActAsync(Admin(), Deliveries, deliveryId, "post", idempotencyKey: key, rowVersion: rowVersion).PostedAsync();

        Assert.Equal((first.Text("number"), first.Text("stockDocumentId")), (replay.Text("number"), replay.Text("stockDocumentId")));
        Assert.Equal((8m, 2m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));
        var other = await DraftDeliveryAsync(admin, orderId, (lines[0], 1m, null)).CreatedIdAsync();
        await (await ActAsync(admin, Deliveries, other, "post", idempotencyKey: key))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Sales.Idempotency.KeyReused");
        await (await ActAsync(admin, Deliveries, other, "post"))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed"); // posting needs a key
        await AssertReconciledAsync(admin, room);
    }

    [Fact]
    public async Task A_loss_below_what_orders_reserved_leaves_each_reservation_alone_and_deliveries_still_need_the_goods()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin, stock: 5m);
        var (orderA, linesA) = await ConfirmedOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 3m));
        var (orderB, _) = await ConfirmedOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 2m));

        var loss = await InventoryTesting.DraftIdAsync(admin, StockDocumentType.Adjustment, room.WarehouseId, AdjustmentReason.Loss,
            InventoryTesting.Line(room.ProductId, room.Piece, 3m, direction: StockDirection.Out));
        await InventoryTesting.ActAsync(admin, loss, "submit").EnsureOkAsync();
        var approved = await InventoryTesting.ActAsync(admin, loss, "approve").PostedAsync();
        Assert.Equal(1, approved.GetProperty("overReservations").GetArrayLength());

        var tooMuch = await DraftDeliveryAsync(admin, orderA, (linesA[0], 3m, null)).CreatedIdAsync();
        await (await ActAsync(admin, Deliveries, tooMuch, "post", idempotencyKey: Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
        await DeliverAsync(admin, orderA, (linesA[0], 2m, null));

        Assert.Equal((0m, 3m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));
        var reservations = (await admin.GetJsonAsync($"/api/inventory/reservations?warehouseId={room.WarehouseId}"))
            .GetProperty("items").EnumerateArray().ToDictionary(r => r.GetProperty("sourceDocumentId").GetGuid(), r => r.Decimal("remainingQuantity"));
        Assert.Equal((1m, 2m), (reservations[orderA], reservations[orderB]));
        var over = await admin.GetJsonAsync($"/api/inventory/over-reservations?warehouseId={room.WarehouseId}");
        Assert.Contains(over.GetProperty("items").EnumerateArray(), o => o.GetProperty("productId").GetGuid() == room.ProductId);
        await AssertReconciledAsync(admin, room);
    }

    [Fact]
    public async Task Reducing_closing_and_cancelling_orders_release_exactly_their_own_reservations()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin, stock: 20m);
        var (orderId, lines) = await ConfirmedOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 5m));
        var (otherId, _) = await ConfirmedOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 4m));
        Assert.Equal((20m, 9m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));

        await (await ReduceAsync(admin, orderId, lines[0], 5m)).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Order.CancelInstead");
        await (await ReduceAsync(admin, orderId, lines[0], 6m)).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Order.ReductionInvalid");
        await (await ActAsync(admin, Orders, orderId, "close", new { reason = "x", rowVersion = await RowVersionAsync(admin, orderId) }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Order.NothingDeliveredToClose");
        await ReduceAsync(admin, orderId, lines[0], 2m).EnsureOkAsync();
        Assert.Equal((20m, 7m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));

        await DeliverAsync(admin, orderId, (lines[0], 1m, null));
        await (await ActAsync(admin, Orders, orderId, "cancel", new { reason = (string?)null, rowVersion = await RowVersionAsync(admin, orderId) }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Order.NotCancellable");
        await ActAsync(admin, Orders, orderId, "close", new { reason = "الباقي غير مطلوب", rowVersion = await RowVersionAsync(admin, orderId) })
            .EnsureOkAsync();
        var line = (await GetAsync(admin, Orders, orderId)).GetProperty("lines")[0];
        Assert.Equal((1m, 4m, 0m), (line.Decimal("deliveredQuantity"), line.Decimal("closedQuantity"), line.Decimal("remainingQuantity")));
        Assert.Equal((19m, 4m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));

        await ActAsync(admin, Orders, otherId, "cancel", new { reason = "ألغاه العميل", rowVersion = await RowVersionAsync(admin, otherId) })
            .EnsureOkAsync();
        var cancelled = await GetAsync(admin, Orders, otherId);
        Assert.Equal("Cancelled", cancelled.Text("status"));
        Assert.NotNull(cancelled.Text("number")); // a confirmed order keeps its number when cancelled
        Assert.Equal((19m, 0m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));
        await AssertReconciledAsync(admin, room);
    }

    private static async Task<string?> RowVersionAsync(HttpClient client, Guid orderId) => (await GetAsync(client, Orders, orderId)).Text("rowVersion");

    private static async Task<HttpResponseMessage> ReduceAsync(HttpClient client, Guid orderId, Guid lineId, decimal quantity) =>
        await ActAsync(client, Orders, orderId, $"lines/{lineId}/reduce",
            new { quantity, reason = "نقص لدى العميل", rowVersion = await RowVersionAsync(client, orderId) });

    // Quantities, reservations and valuation agree for the product after the race.
    private static async Task AssertReconciledAsync(HttpClient admin, Stockroom room)
    {
        var report = await admin.GetJsonAsync("/api/inventory/reconciliation");
        foreach (var section in new[] { "availabilityDifferences", "ledgerDifferences", "valuationDifferences", "reservationDifferences" })
            Assert.DoesNotContain(report.GetProperty(section).EnumerateArray(), d => d.GetProperty("productId").GetGuid() == room.ProductId);
    }
}
