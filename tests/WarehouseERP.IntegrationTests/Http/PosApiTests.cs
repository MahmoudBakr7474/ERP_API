using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Sales.Contracts;
using static WarehouseERP.IntegrationTests.Http.PosTesting;
using CashMovement = WarehouseERP.Pos.Contracts.PosCashMovementType;

namespace WarehouseERP.IntegrationTests.Http;

// POS V1 through the real HTTP pipeline on SQL Server: terminals, cashier shifts and their drawers, checkout with cash,
// card and split payments, Sales' prices and product tax classifications, discount limits, receipts and counted copies,
// returns and refunds at the original price and cost, card incidents, count freezes, Inventory's protection of POS stock
// documents, and permissions and warehouse scope (self-approval: a cashier with the permission refunds their own sale).
[Collection(DatabaseCollection.Name)]
public sealed class PosApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    // Every test acts through its own users, so a cashier's single open shift never clashes with another test's.
    private HttpClient Admin(Guid? user = null) => _api.CreateClient().As(AllPermissions, userId: user ?? Guid.NewGuid());

    private HttpClient Cashier(Guid user, string? permissions = null, string warehouses = "*") =>
        _api.CreateClient().As(permissions ?? CashierPermissions, warehouses, user);

    private static async Task<Guid> OpenAsync(HttpClient client, Guid terminalId, decimal openingFloat = 100m) =>
        await OpenShiftAsync(client, terminalId, openingFloat).CreatedIdAsync();

    [Fact]
    public async Task A_cash_sale_posts_its_stock_numbers_its_receipt_and_fills_the_drawer_with_the_net_cash()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        var cashierId = Guid.NewGuid();
        var cashier = Cashier(cashierId);
        var shiftId = await OpenAsync(cashier, till.TerminalId);

        var receipt = await CheckoutAsync(cashier, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 2m)), Pay(Cash(50m)))
            .SoldAsync();

        Assert.Equal($"{till.TerminalCode}-2026-000001", receipt.Text("number"));
        Assert.False(receipt.GetProperty("isTaxInvoice").GetBoolean());
        Assert.False(receipt.GetProperty("isCopy").GetBoolean());
        Assert.Equal((20m, 2.80m, 22.80m), (receipt.Decimal("netAmount"), receipt.Decimal("taxAmount"), receipt.Decimal("totalAmount")));
        Assert.Equal((50m, 27.20m, 22.80m, 0m),
            (receipt.Decimal("cashTenderedAmount"), receipt.Decimal("changeAmount"), receipt.Decimal("cashReceivedAmount"), receipt.Decimal("cardAmount")));
        var line = receipt.GetProperty("lines")[0];
        Assert.Equal((10m, 11.40m, "Standard", 14m), (line.Decimal("unitPrice"), line.Decimal("unitPriceWithTax"), line.Text("taxTreatment"), line.Decimal("taxRate")));
        var tax = Assert.Single(receipt.GetProperty("taxBreakdown").EnumerateArray());
        Assert.Equal((20m, 2.80m), (tax.Decimal("netAmount"), tax.Decimal("taxAmount")));
        var payment = Assert.Single(receipt.GetProperty("payments").EnumerateArray());
        Assert.Equal(("Cash", "Settled", 22.80m, 50m, 27.20m),
            (payment.Text("method"), payment.Text("status"), payment.Decimal("amount"), payment.Decimal("tenderedAmount"), payment.Decimal("changeAmount")));
        Assert.Equal(("متجر الاختبار", "Africa/Cairo"), (receipt.Text("receiptHeader"), receipt.Text("timeZoneId")));
        Assert.StartsWith("PSL-", receipt.Text("stockDocumentNumber"));

        Assert.Equal(8m, await InventoryTesting.BalanceAsync(admin, till.Room));
        await InventoryTesting.AssertValuedAsync(admin, till.Room.ProductId, 8m, 48m);
        var stockDocument = await InventoryTesting.DocumentAsync(admin, receipt.GetProperty("stockDocumentId").GetGuid());
        Assert.Equal(("PosSale", "Posted"), (stockDocument.Text("documentType"), stockDocument.Text("status")));

        var shift = await ShiftAsync(cashier, shiftId);
        Assert.Equal((100m, 22.80m, 122.80m, 22.80m, 1),
            (shift.Decimal("openingFloat"), shift.Decimal("cashSalesAmount"), shift.Decimal("expectedCash"), shift.Decimal("salesAmount"),
                shift.GetProperty("salesCount").GetInt32()));
        Assert.Equal(shiftId, (await CurrentShiftAsync(cashier, till.TerminalId)).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Inventory_never_drafts_edits_or_reverses_a_POS_stock_document()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        await OpenAsync(admin, till.TerminalId);
        var receipt = await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m)))
            .SoldAsync();

        await (await InventoryTesting.ActAsync(admin, receipt.GetProperty("stockDocumentId").GetGuid(), "reverse"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.ManagedByPos");
        await (await InventoryTesting.DraftAsync(admin, WarehouseERP.Inventory.Contracts.StockDocumentType.PosSale, till.Room.WarehouseId, null,
                InventoryTesting.Line(till.Room.ProductId, till.Room.Piece, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.ManagedByPos");
    }

    [Fact]
    public async Task A_retried_checkout_returns_the_first_receipt_and_never_sells_twice()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        await OpenAsync(admin, till.TerminalId);
        var key = Guid.NewGuid();
        var lines = Lines(CartLine(till.Room.ProductId, till.Room.Piece, 2m));

        var first = await CheckoutAsync(admin, till.TerminalId, lines, Pay(Cash(22.80m)), key).SoldAsync();
        var retry = await CheckoutAsync(admin, till.TerminalId, lines, Pay(Cash(22.80m)), key).SoldAsync();

        Assert.Equal((first.GetProperty("id").GetGuid(), first.Text("number")), (retry.GetProperty("id").GetGuid(), retry.Text("number")));
        Assert.False(retry.GetProperty("isCopy").GetBoolean());
        Assert.Equal(8m, await InventoryTesting.BalanceAsync(admin, till.Room));
        await (await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m)), key))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Pos.Idempotency.KeyReused");
        await (await CheckoutAsync(admin, till.TerminalId, lines, Pay(Cash(22.80m)), withoutKey: true))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
    }

    [Fact]
    public async Task A_split_payment_keeps_cards_out_of_the_drawer_and_change_comes_from_cash_only()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        var shiftId = await OpenAsync(admin, till.TerminalId);

        var receipt = await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 2m)),
            Pay(Card(10m), Cash(20m))).SoldAsync();

        Assert.Equal((10m, 20m, 7.20m, 12.80m),
            (receipt.Decimal("cardAmount"), receipt.Decimal("cashTenderedAmount"), receipt.Decimal("changeAmount"), receipt.Decimal("cashReceivedAmount")));
        var card = receipt.GetProperty("payments").EnumerateArray().Single(p => p.Text("method") == "Card");
        Assert.Equal(("ExternallyConfirmed", "AUTH-123456", "4242"), (card.Text("status"), card.Text("reference"), card.Text("cardLast4")));
        var shift = await ShiftAsync(admin, shiftId);
        Assert.Equal((12.80m, 10m, 112.80m), (shift.Decimal("cashSalesAmount"), shift.Decimal("cardSalesAmount"), shift.Decimal("expectedCash")));

        await (await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Card(11.40m, reference: null))))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        await (await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Card(11.40m, last4: "4111111111111111"))))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        await (await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(10m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Payment.Underpaid");
        Assert.Equal(8m, await InventoryTesting.BalanceAsync(admin, till.Room));
    }

    [Fact]
    public async Task A_card_charged_for_a_refused_checkout_is_kept_as_an_incident_until_a_retry_records_the_sale_or_someone_resolves_it()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin, stock: 1m);
        var shiftId = await OpenAsync(admin, till.TerminalId);
        var key = Guid.NewGuid();
        var lines = Lines(CartLine(till.Room.ProductId, till.Room.Piece, 2m));

        var refused = await (await CheckoutAsync(admin, till.TerminalId, lines, Pay(Card(22.80m)), key))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Checkout.CardReversalRequired");
        Assert.Contains("Inventory.Stock.Insufficient", refused.Text("detail"));
        Assert.Contains("AUTH-123456 (*4242)", refused.Text("detail"));
        var incident = (await admin.GetJsonAsync($"{CardIncidents}?terminalId={till.TerminalId}")).GetProperty("items").EnumerateArray().Single();
        Assert.Equal(("ReversalRequired", 22.80m, "Inventory.Stock.Insufficient", shiftId),
            (incident.Text("status"), incident.Decimal("cardAmount"), incident.Text("errorCode"), incident.GetProperty("shiftId").GetGuid()));
        Assert.Equal(1, (await ShiftAsync(admin, shiftId)).GetProperty("openCardIncidents").GetInt32());
        Assert.Equal((1m, 0), (await InventoryTesting.BalanceAsync(admin, till.Room),
            (await admin.GetJsonAsync($"{SalesRoute}?terminalId={till.TerminalId}")).GetProperty("totalCount").GetInt32()));
        await (await CheckoutAsync(admin, till.TerminalId, lines, Pay(Card(22.80m)), key))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Checkout.CardReversalRequired");

        // More stock arrives; the cashier retries the same checkout and the sale is recorded against the same charge.
        await InventoryTesting.ReceiveAsync(admin, till.Room, 5m, 6m);
        await CheckoutAsync(admin, till.TerminalId, lines, Pay(Card(22.80m)), key).SoldAsync();
        incident = (await admin.GetJsonAsync($"{CardIncidents}?terminalId={till.TerminalId}")).GetProperty("items").EnumerateArray().Single();
        Assert.Equal(("SaleRecorded", 2), (incident.Text("status"), incident.GetProperty("attempts").GetInt32()));

        // A charge nobody retries is reversed on the card terminal and resolved with a note.
        await (await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 50m)), Pay(Card(570m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Checkout.CardReversalRequired");
        var open = (await admin.GetJsonAsync($"{CardIncidents}?terminalId={till.TerminalId}&status=ReversalRequired"))
            .GetProperty("items").EnumerateArray().Single();
        await (await admin.PostJsonAsync($"{CardIncidents}/{open.GetProperty("id").GetGuid()}/resolve", new { note = (string?)null, rowVersion = open.Text("rowVersion") }))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        await admin.PostJsonAsync($"{CardIncidents}/{open.GetProperty("id").GetGuid()}/resolve",
            new { note = "Voided on the card terminal, ref 998877", rowVersion = open.Text("rowVersion") }).EnsureOkAsync();
        Assert.Equal(0, (await ShiftAsync(admin, shiftId)).GetProperty("openCardIncidents").GetInt32());
    }

    [Fact]
    public async Task Tax_comes_from_the_products_classification_and_an_unclassified_or_unpriced_item_is_refused()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        await OpenAsync(admin, till.TerminalId);
        var piece = Lines(CartLine(till.Room.ProductId, till.Room.Piece, 2m));

        await admin.DeleteAsync($"{ProductTaxes}/{till.Room.ProductId}");
        await (await CheckoutAsync(admin, till.TerminalId, piece, Pay(Cash(30m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.ProductTax.NotClassified");

        await ClassifyAsync(admin, till.Room.ProductId, SalesTaxTreatment.ZeroRated);
        var zero = await CheckoutAsync(admin, till.TerminalId, piece, Pay(Cash(20m))).SoldAsync();
        Assert.Equal((0m, 20m, "ZeroRated"), (zero.Decimal("taxAmount"), zero.Decimal("totalAmount"), zero.GetProperty("lines")[0].Text("taxTreatment")));

        await ClassifyAsync(admin, till.Room.ProductId, SalesTaxTreatment.Exempt);
        var exempt = await CheckoutAsync(admin, till.TerminalId, piece, Pay(Cash(20m))).SoldAsync();
        Assert.Equal(("Exempt", 0m), (exempt.GetProperty("lines")[0].Text("taxTreatment"), exempt.Decimal("taxAmount")));

        await ClassifyAsync(admin, till.Room.ProductId, SalesTaxTreatment.Standard, 5m);
        var ownRate = await CheckoutAsync(admin, till.TerminalId, piece, Pay(Cash(21m))).SoldAsync();
        Assert.Equal((5m, 1m, 21m), (ownRate.GetProperty("lines")[0].Decimal("taxRate"), ownRate.Decimal("taxAmount"), ownRate.Decimal("totalAmount")));

        await (await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Carton, 1m)), Pay(Cash(200m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Line.PriceMissing");
        await ClassifyAsync(admin, till.Room.ProductId, SalesTaxTreatment.Standard);
    }

    [Fact]
    public async Task Items_are_scanned_and_searched_with_their_price_VAT_and_stock_and_say_why_they_cannot_be_sold()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        var barcode = "2" + Random.Shared.NextInt64(100_000_000_000, 999_999_999_999).ToString(System.Globalization.CultureInfo.InvariantCulture);
        (await CatalogTesting.AddBarcodeAsync(admin, till.Room.ProductId, till.Room.Piece, barcode)).EnsureSuccessStatusCode();
        var cashier = Cashier(Guid.NewGuid());

        var scanned = await cashier.GetJsonAsync($"{Terminals}/{till.TerminalId}/items/lookup?barcode={barcode}");
        Assert.Equal((till.Room.Piece, 10m, 11.40m, 10m, true),
            (scanned.GetProperty("productUnitId").GetGuid(), scanned.Decimal("unitPrice"), scanned.Decimal("unitPriceWithTax"),
                scanned.Decimal("availableQuantity"), scanned.GetProperty("canSell").GetBoolean()));
        await (await cashier.GetAsync($"{Terminals}/{till.TerminalId}/items/lookup?barcode=000000000"))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Pos.Item.NotFound");

        var sku = (await admin.GetJsonAsync($"{CatalogTesting.Products}/{till.Room.ProductId}")).Text("sku");
        var found = (await cashier.GetJsonAsync($"{Terminals}/{till.TerminalId}/items?search={sku}")).EnumerateArray().ToList();
        Assert.Equal(2, found.Count);
        var carton = found.Single(i => i.GetProperty("productUnitId").GetGuid() == till.Room.Carton);
        Assert.Equal((false, "Sales.Line.PriceMissing", 0.8333m),
            (carton.GetProperty("canSell").GetBoolean(), carton.Text("problemCode"), carton.Decimal("availableQuantity")));
    }

    [Fact]
    public async Task Discounts_need_the_permission_and_stay_within_the_settings_limit()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        var plainId = Guid.NewGuid();
        var plain = Cashier(plainId, string.Join(',', PosPermissions.Sell, PosPermissions.Read));
        await OpenAsync(plain, till.TerminalId);

        await (await CheckoutAsync(plain, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 2m, 5m)), Pay(Cash(30m))))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Pos.Sale.DiscountNotAllowed");

        var other = await CreateTillAsync(fx, admin);
        var cashier = Cashier(Guid.NewGuid());
        await OpenAsync(cashier, other.TerminalId);
        await (await CheckoutAsync(cashier, other.TerminalId, Lines(CartLine(other.Room.ProductId, other.Room.Piece, 2m, 15m)), Pay(Cash(30m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Sale.DiscountAboveMax");
        var discounted = await CheckoutAsync(cashier, other.TerminalId, Lines(CartLine(other.Room.ProductId, other.Room.Piece, 2m, 10m)), Pay(Cash(20.52m)))
            .SoldAsync();
        Assert.Equal((20m, 2m, 18m, 2.52m, 20.52m),
            (discounted.Decimal("grossAmount"), discounted.Decimal("discountAmount"), discounted.Decimal("netAmount"), discounted.Decimal("taxAmount"),
                discounted.Decimal("totalAmount")));
    }

    [Fact]
    public async Task One_open_shift_per_terminal_and_per_cashier_and_only_its_cashier_sells_on_it()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        var second = await CreateTillAsync(fx, admin);
        var aliceId = Guid.NewGuid();
        var alice = Cashier(aliceId);
        var bob = Cashier(Guid.NewGuid());
        await OpenAsync(alice, till.TerminalId);

        await (await OpenShiftAsync(bob, till.TerminalId)).AssertProblemAsync(HttpStatusCode.Conflict, "Pos.Shift.TerminalBusy");
        await (await OpenShiftAsync(alice, second.TerminalId)).AssertProblemAsync(HttpStatusCode.Conflict, "Pos.Shift.CashierBusy");
        await (await CheckoutAsync(bob, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m))))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Pos.Access.NotShiftCashier");
        await (await bob.GetAsync($"{Terminals}/{till.TerminalId}/current-shift")).AssertProblemAsync(HttpStatusCode.NotFound, "Pos.Shift.NotFound");
        await (await admin.PostAsync($"{Terminals}/{till.TerminalId}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Terminal.HasOpenShift");
    }

    [Fact]
    public async Task The_drawer_reconciles_float_sales_cash_in_and_out_and_a_count_difference_needs_a_note()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        var cashier = Cashier(Guid.NewGuid());
        var shiftId = await OpenAsync(cashier, till.TerminalId, 200m);
        await CheckoutAsync(cashier, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(20m))).SoldAsync();
        await CheckoutAsync(cashier, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Card(11.40m))).SoldAsync();

        (await CashMovementAsync(cashier, shiftId, CashMovement.CashIn, 50m, "فكة")).EnsureSuccessStatusCode();
        (await CashMovementAsync(cashier, shiftId, CashMovement.CashOut, 100m, "إيداع الخزينة")).EnsureSuccessStatusCode();
        await (await CashMovementAsync(cashier, shiftId, CashMovement.CashOut, 1000m, "كثير"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Shift.DrawerShort");
        var stale = (await ShiftAsync(cashier, shiftId)).Text("rowVersion");
        await CheckoutAsync(cashier, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m))).SoldAsync();
        await (await cashier.PostJsonAsync($"{Shifts}/{shiftId}/cash-movements", new { movementType = CashMovement.CashIn, amount = 5m, reason = "late", rowVersion = stale }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");

        var shift = await ShiftAsync(cashier, shiftId);
        var expected = 200m + 11.40m + 11.40m + 50m - 100m;
        Assert.Equal((expected, 11.40m, 2), (shift.Decimal("expectedCash"), shift.Decimal("cardSalesAmount"), shift.GetProperty("cashMovements").GetArrayLength()));

        await (await CloseShiftAsync(cashier, shiftId, expected - 3m, null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Shift.DifferenceNoteRequired");
        var closed = await CloseShiftAsync(cashier, shiftId, expected - 3m, "3 ج ناقص").SoldAsync();
        Assert.Equal(("Closed", expected, expected - 3m, -3m, false),
            (closed.Text("status"), closed.Decimal("expectedCash"), closed.Decimal("countedCash"), closed.Decimal("cashDifference"),
                closed.GetProperty("isForceClosed").GetBoolean()));
        await (await CheckoutAsync(cashier, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Shift.NoOpenShift");
        await (await CloseShiftAsync(cashier, shiftId, expected, "again")).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Shift.NotOpen");
    }

    [Fact]
    public async Task A_shift_manager_force_closes_an_abandoned_shift_with_a_note_and_nobody_else_may()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        var cashier = Cashier(Guid.NewGuid());
        var shiftId = await OpenAsync(cashier, till.TerminalId, 80m);
        var colleague = Cashier(Guid.NewGuid());
        var manager = Cashier(Guid.NewGuid(), string.Join(',', PosPermissions.Read, PosPermissions.ShiftsManage));

        await (await CloseShiftAsync(colleague, shiftId, 80m, "leaving")).AssertProblemAsync(HttpStatusCode.Forbidden, "Pos.Access.NotShiftCashier");
        await (await CloseShiftAsync(manager, shiftId, 80m, null)).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Shift.ForceCloseNoteRequired");
        var closed = await CloseShiftAsync(manager, shiftId, 80m, "الكاشير غادر دون إغلاق").SoldAsync();

        Assert.True(closed.GetProperty("isForceClosed").GetBoolean());
        Assert.Equal("ForceClosed", closed.GetProperty("events").EnumerateArray().Last().Text("action"));
        await OpenAsync(colleague, till.TerminalId);
    }

    [Fact]
    public async Task Returns_refund_at_the_sold_price_restock_at_the_sold_cost_and_never_exceed_what_was_sold()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        var cashier = Cashier(Guid.NewGuid());
        var shiftId = await OpenAsync(cashier, till.TerminalId);
        var sale = await CheckoutAsync(cashier, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 3m)), Pay(Cash(40m))).SoldAsync();
        var saleId = sale.GetProperty("id").GetGuid();
        var lineId = sale.GetProperty("lines")[0].GetProperty("id").GetGuid();
        await SalesTesting.SetPriceAsync(admin, till.Room.Piece, 99m); // a later price change never touches a refund

        var first = await ReturnAsync(cashier, till.TerminalId, saleId, Lines(ReturnLine(lineId, 1m)), Pay(Cash(11.40m))).SoldAsync();
        Assert.Equal(($"{till.TerminalCode}-R-2026-000001", 10m, 1.40m, 11.40m),
            (first.Text("number"), first.Decimal("netAmount"), first.Decimal("taxAmount"), first.Decimal("totalAmount")));
        Assert.StartsWith("PSR-", first.Text("stockDocumentNumber"));
        Assert.Equal(11.40m, first.GetProperty("lines")[0].Decimal("unitPriceWithTax"));
        var refundedTax = Assert.Single(first.GetProperty("taxBreakdown").EnumerateArray());
        Assert.Equal(("Standard", 14m, 10m, 1.40m), (refundedTax.Text("taxTreatment"), refundedTax.Decimal("taxRate"), refundedTax.Decimal("netAmount"),
            refundedTax.Decimal("taxAmount")));
        Assert.Equal(8m, await InventoryTesting.BalanceAsync(admin, till.Room));
        await InventoryTesting.AssertValuedAsync(admin, till.Room.ProductId, 8m, 48m);

        await (await ReturnAsync(cashier, till.TerminalId, saleId, Lines(ReturnLine(lineId, 3m)), Pay(Cash(34.20m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Return.ExceedsSale");
        await (await ReturnAsync(cashier, till.TerminalId, saleId, Lines(ReturnLine(lineId, 1m, PosReturnCondition.Damaged)), Pay(Cash(11.40m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Return.ConditionNotSupported");
        await (await ReturnAsync(cashier, till.TerminalId, saleId, Lines(ReturnLine(lineId, 1m)), Pay(Cash(10m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Refund.Mismatch");
        await (await ReturnAsync(cashier, till.TerminalId, saleId, Lines(ReturnLine(lineId, 1m)), Pay(Card(11.40m, "RFD-1"))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Refund.CardExceedsPaid");

        await ReturnAsync(cashier, till.TerminalId, saleId, Lines(ReturnLine(lineId, 2m)), Pay(Cash(22.80m))).SoldAsync();
        var after = await admin.GetJsonAsync($"{SalesRoute}/{saleId}");
        Assert.Equal((34.20m, 34.20m, 0m), (after.Decimal("refundedAmount"), after.Decimal("refundedCashAmount"), after.Decimal("refundedCardAmount")));
        Assert.Equal((34.20m, 3m, 0m, 2),
            (after.Decimal("refundedAmount"), after.GetProperty("lines")[0].Decimal("returnedQuantity"),
                after.GetProperty("lines")[0].Decimal("returnableQuantity"), after.GetProperty("returns").GetArrayLength()));
        Assert.Equal(10m, await InventoryTesting.BalanceAsync(admin, till.Room));
        await InventoryTesting.AssertValuedAsync(admin, till.Room.ProductId, 10m, 60m);
        var shift = await ShiftAsync(cashier, shiftId);
        Assert.Equal((34.20m, 34.20m, 100m, 2), (shift.Decimal("cashSalesAmount"), shift.Decimal("cashRefundsAmount"), shift.Decimal("expectedCash"),
            shift.GetProperty("returnsCount").GetInt32()));
    }

    [Fact]
    public async Task Goods_come_back_only_within_the_return_window()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        var cashier = Cashier(Guid.NewGuid());
        await OpenAsync(cashier, till.TerminalId);
        var sale = await CheckoutAsync(cashier, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m))).SoldAsync();
        var saleId = sale.GetProperty("id").GetGuid();
        await using (var db = fx.NewDb())
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [pos].[PosSales] SET [SoldAtUtc] = DATEADD(day, -15, [SoldAtUtc]) WHERE [Id] = {saleId}");

        await (await ReturnAsync(cashier, till.TerminalId, saleId, Lines(ReturnLine(sale.GetProperty("lines")[0].GetProperty("id").GetGuid(), 1m)),
                Pay(Cash(11.40m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Return.WindowClosed");
    }

    [Fact]
    public async Task A_count_freezing_the_selling_location_stops_sales_until_it_is_cancelled()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        await OpenAsync(admin, till.TerminalId);
        var countId = await InventoryTesting.DraftCountAsync(admin, till.Room.WarehouseId, till.Room.DefaultLocationId).CreatedIdAsync();
        await InventoryTesting.CountActAsync(admin, countId, "start").EnsureOkAsync();

        await (await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockCount.ScopeFrozen");
        await InventoryTesting.CountActAsync(admin, countId, "cancel").EnsureOkAsync();
        await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m))).SoldAsync();
    }

    [Fact]
    public async Task Copies_of_receipts_are_counted_and_marked_and_need_their_permission()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        var cashier = Cashier(Guid.NewGuid());
        await OpenAsync(cashier, till.TerminalId);
        var saleId = (await CheckoutAsync(cashier, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m))).SoldAsync())
            .GetProperty("id").GetGuid();

        await (await cashier.PostAsync($"{SalesRoute}/{saleId}/reprint", null)).AssertStatusAsync(HttpStatusCode.Forbidden);
        var copy = await admin.PostAsync($"{SalesRoute}/{saleId}/reprint", null).SoldAsync();
        var second = await admin.PostAsync($"{SalesRoute}/{saleId}/reprint", null).SoldAsync();
        Assert.Equal((true, 1, 2), (copy.GetProperty("isCopy").GetBoolean(), copy.GetProperty("copyNumber").GetInt32(), copy.GetProperty("printCount").GetInt32()));
        Assert.Equal((2, 3), (second.GetProperty("copyNumber").GetInt32(), second.GetProperty("printCount").GetInt32()));
    }

    [Fact]
    public async Task Tills_and_their_records_follow_the_warehouse_scope_and_reading_needs_its_permission()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        await OpenAsync(admin, till.TerminalId);
        var saleId = (await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m))).SoldAsync())
            .GetProperty("id").GetGuid();
        var elsewhere = Cashier(Guid.NewGuid(), warehouses: Guid.NewGuid().ToString());

        await (await OpenShiftAsync(elsewhere, till.TerminalId)).AssertProblemAsync(HttpStatusCode.Forbidden, "Pos.Access.WarehouseDenied");
        await (await elsewhere.GetAsync($"{SalesRoute}/{saleId}")).AssertProblemAsync(HttpStatusCode.Forbidden, "Pos.Access.WarehouseDenied");
        await (await elsewhere.GetAsync($"{Terminals}/{till.TerminalId}/items/lookup?barcode=1"))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Pos.Access.WarehouseDenied");
        Assert.DoesNotContain((await elsewhere.GetJsonAsync($"{SalesRoute}?pageSize=100")).GetProperty("items").EnumerateArray(),
            s => s.GetProperty("id").GetGuid() == saleId);

        var sellerOnly = Cashier(Guid.NewGuid(), PosPermissions.Sell);
        await (await sellerOnly.GetAsync($"{SalesRoute}/{saleId}")).AssertStatusAsync(HttpStatusCode.Forbidden);
        await (await sellerOnly.PostJsonAsync(Terminals, new { code = "X1", name = "x", warehouseId = till.Room.WarehouseId, acceptsCash = true, acceptsCard = false }))
            .AssertStatusAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Terminals_sell_from_a_selling_warehouse_have_unique_fixed_codes_and_take_at_least_one_payment_method()
    {
        var admin = Admin();
        var till = await CreateTillAsync(fx, admin);
        var terminal = await admin.GetJsonAsync($"{Terminals}/{till.TerminalId}");
        Assert.Equal((till.Room.DefaultLocationId, true, true), (terminal.GetProperty("sellingLocationId").GetGuid(),
            terminal.GetProperty("acceptsCash").GetBoolean(), terminal.GetProperty("acceptsCard").GetBoolean()));

        await (await admin.PostJsonAsync(Terminals, new { code = till.TerminalCode.ToLowerInvariant(), name = "مكرر", warehouseId = till.Room.WarehouseId, acceptsCash = true, acceptsCard = true }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Pos.Terminal.CodeTaken");
        await (await admin.PostJsonAsync(Terminals, new { code = "NOPAY", name = "x", warehouseId = till.Room.WarehouseId, acceptsCash = false, acceptsCard = false }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Terminal.NoPaymentMethod");
        var transit = await InventoryTesting.CreateWarehouseAsync(admin, till.Room.BranchId, "Transit");
        await (await admin.PostJsonAsync(Terminals, new { code = DatabaseFixture.Code("T")[..10], name = "x", warehouseId = transit, acceptsCash = true, acceptsCard = false }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Pos.Terminal.WarehouseNotSelling");

        var shelf = await InventoryTesting.AddLocationAsync(admin, till.Room.WarehouseId);
        await admin.PutJsonAsync($"{Terminals}/{till.TerminalId}", new
        {
            name = "كاشير الصالة", sellingLocationId = shelf, acceptsCash = true, acceptsCard = false, receiptHeader = (string?)null,
            receiptFooter = (string?)null, rowVersion = terminal.Text("rowVersion"),
        }).EnsureOkAsync();
        var updated = await admin.GetJsonAsync($"{Terminals}/{till.TerminalId}");
        Assert.Equal((till.TerminalCode, shelf, false), (updated.Text("code"), updated.GetProperty("sellingLocationId").GetGuid(),
            updated.GetProperty("acceptsCard").GetBoolean()));

        await OpenAsync(admin, till.TerminalId);
        await (await CheckoutAsync(admin, till.TerminalId, Lines(CartLine(till.Room.ProductId, till.Room.Piece, 1m)), Pay(Cash(11.40m))))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.InsufficientAtLocation");
    }

    [Fact]
    public async Task The_settings_need_a_company_wide_user_and_bound_the_till_discount()
    {
        var admin = Admin();
        var settings = await admin.GetJsonAsync(Settings);
        Assert.Equal((10m, 14), (settings.Decimal("maxLineDiscountPercent"), settings.GetProperty("returnWindowDays").GetInt32()));

        var scoped = Cashier(Guid.NewGuid(), PosPermissions.SettingsManage, Guid.NewGuid().ToString());
        await (await scoped.PutJsonAsync(Settings, new { maxLineDiscountPercent = 20m, returnWindowDays = 30, rowVersion = settings.Text("rowVersion") }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Pos.Access.CompanyWideRequired");
        await (await admin.PutJsonAsync(Settings, new { maxLineDiscountPercent = 101m, returnWindowDays = 30, rowVersion = settings.Text("rowVersion") }))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        var changed = await (await admin.PutJsonAsync(Settings, new { maxLineDiscountPercent = 12.5m, returnWindowDays = 30, rowVersion = settings.Text("rowVersion") }))
            .ReadAsync<JsonElement>();
        Assert.Equal((12.5m, 30), ((await admin.GetJsonAsync(Settings)).Decimal("maxLineDiscountPercent"),
            (await admin.GetJsonAsync(Settings)).GetProperty("returnWindowDays").GetInt32()));
        await (await admin.PutJsonAsync(Settings, new { maxLineDiscountPercent = 10m, returnWindowDays = 14, rowVersion = settings.Text("rowVersion") }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");
        (await admin.PutJsonAsync(Settings, new { maxLineDiscountPercent = 10m, returnWindowDays = 14, rowVersion = changed.Text("rowVersion") }))
            .EnsureSuccessStatusCode(); // back to the seeded defaults the other tests expect
    }
}

internal static class PosHttpAssertions
{
    public static async Task AssertStatusAsync(this HttpResponseMessage response, HttpStatusCode status) =>
        Assert.True(response.StatusCode == status, $"Expected {(int)status}, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
}
