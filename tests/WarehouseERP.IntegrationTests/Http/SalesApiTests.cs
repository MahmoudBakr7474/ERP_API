using System.Net;
using System.Text.Json;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Sales.Contracts;
using static WarehouseERP.IntegrationTests.Http.SalesTesting;
using Stockroom = WarehouseERP.IntegrationTests.Http.InventoryTesting.Stockroom;

namespace WarehouseERP.IntegrationTests.Http;

// Sales V1 through the real host on SQL Server: quotation to order to reservation, partial deliveries from several
// locations, partial invoicing, returns at their delivery's cost, approvals (self-approval included), pricing rules,
// credit holds, customers and warehouse scope.
[Collection(DatabaseCollection.Name)]
public sealed class SalesApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(AllPermissions);

    private HttpClient User(Guid warehouseId, params string[] permissions) =>
        _api.CreateClient().As(string.Join(',', permissions), warehouseId.ToString(), Guid.NewGuid());

    private async Task<(Stockroom Room, Guid CustomerId)> SetUpAsync(HttpClient admin, decimal stock = 10m, decimal cost = 6m, decimal price = 10m)
    {
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        if (stock > 0)
            await InventoryTesting.ReceiveAsync(admin, room, stock, cost);
        await SetPriceAsync(admin, room.Piece, price);
        return (room, await CreateCustomerAsync(admin));
    }

    [Fact]
    public async Task A_quotation_becomes_an_order_that_reserves_delivers_invoices_and_takes_goods_back_at_their_cost()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin);

        var quotationId = await admin.PostJsonAsync(Quotations, new
        {
            customerId, warehouseId = room.WarehouseId, quotationDate = (DateOnly?)null, validUntil = (DateOnly?)null,
            customerReference = "RFQ-9", paymentTermDays = (int?)null, notes = (string?)null,
            lines = new[] { Line(room.ProductId, room.Piece, 4m, discountPercent: 10m) },
        }).CreatedIdAsync();
        var quotation = await GetAsync(admin, Quotations, quotationId);
        Assert.Equal((40m, 4m, 36m, 5.04m, 41.04m), (quotation.Decimal("grossAmount"), quotation.Decimal("discountAmount"),
            quotation.Decimal("netAmount"), quotation.Decimal("taxAmount"), quotation.Decimal("totalAmount")));
        var quotedLine = quotation.GetProperty("lines")[0];
        Assert.Equal(("PriceList", 10m, "Standard", 14m), (quotedLine.Text("priceSource"), quotedLine.Decimal("listUnitPrice"),
            quotedLine.Text("taxTreatment"), quotedLine.Decimal("taxRate")));
        await ActAsync(admin, Quotations, quotationId, "submit").EnsureOkAsync();
        Assert.Equal("Issued", (await GetAsync(admin, Quotations, quotationId)).Text("status"));
        Assert.Equal((10m, 0m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));

        await SetPriceAsync(admin, room.Piece, 99m); // a later list price never touches the quotation or its order
        var orderId = await ActAsync(admin, Quotations, quotationId, "convert").CreatedIdAsync();
        Assert.Equal("Converted", (await GetAsync(admin, Quotations, quotationId)).Text("status"));
        await (await ActAsync(admin, Quotations, quotationId, "convert")).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Quotation.NotIssued");
        var draft = await GetAsync(admin, Orders, orderId);
        Assert.Equal((quotationId, 36m, 10m), (draft.GetProperty("sourceQuotationId").GetGuid(), draft.Decimal("netAmount"),
            draft.GetProperty("lines")[0].Decimal("unitPrice")));

        await ActAsync(admin, Orders, orderId, "submit").EnsureOkAsync();
        var order = await GetAsync(admin, Orders, orderId);
        Assert.Equal("Confirmed", order.Text("status"));
        Assert.StartsWith("SO-", order.Text("number"));
        var lineId = order.GetProperty("lines")[0].GetProperty("id").GetGuid();
        Assert.Equal((10m, 4m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));

        var deliveryId = await DeliverAsync(admin, orderId, (lineId, 3m, null));
        var delivery = await GetAsync(admin, Deliveries, deliveryId);
        Assert.Equal(("Posted", 18m), (delivery.Text("status"), delivery.Decimal("costAmount")));
        Assert.StartsWith("SDL-", delivery.Text("stockDocumentNumber"));
        Assert.Equal((7m, 1m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));
        Assert.Equal("PartiallyDelivered", (await GetAsync(admin, Orders, orderId)).Text("status"));

        var invoiceId = await DraftInvoiceAsync(admin, orderId, (lineId, 2m)).CreatedIdAsync();
        var posted = await ActAsync(admin, Invoices, invoiceId, "post", idempotencyKey: Guid.NewGuid()).PostedAsync();
        Assert.Equal((20.52m, Today), (posted.Decimal("totalAmount"), DateOnly.Parse(posted.Text("dueDate")!, System.Globalization.CultureInfo.InvariantCulture)));
        var invoice = await GetAsync(admin, Invoices, invoiceId);
        Assert.Equal((20m, 2m, 18m, 2.52m), (invoice.Decimal("grossAmount"), invoice.Decimal("discountAmount"), invoice.Decimal("netAmount"),
            invoice.Decimal("taxAmount")));
        await (await DraftInvoiceAsync(admin, orderId, (lineId, 2m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Order.InvoiceExceedsDeliveries");

        var deliveryLineId = delivery.GetProperty("lines")[0].GetProperty("id").GetGuid();
        var returnId = await DraftReturnAsync(admin, deliveryId, (deliveryLineId, 2m, ReturnCondition.FitForResale)).CreatedIdAsync();
        await ActAsync(admin, Returns, returnId, "submit").EnsureOkAsync();
        var approved = await ActAsync(admin, Returns, returnId, "approve", idempotencyKey: Guid.NewGuid()).PostedAsync();
        Assert.Equal((12m, 10.26m), (approved.Decimal("costAmount"), approved.Decimal("creditTotalAmount")));
        var returned = await GetAsync(admin, Returns, returnId);
        var returnedLine = returned.GetProperty("lines")[0];
        Assert.Equal((1m, 1m, 9m, 1.26m), (returnedLine.Decimal("uninvoicedQuantity"), returnedLine.Decimal("invoicedQuantity"),
            returnedLine.Decimal("creditNetAmount"), returnedLine.Decimal("creditTaxAmount")));
        Assert.Equal((9m, 1m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));
        await InventoryTesting.AssertValuedAsync(admin, room.ProductId, 9m, 54m);

        await ActAsync(admin, Orders, orderId, "close", new { reason = "العميل لا يريد الباقي",
            rowVersion = (await GetAsync(admin, Orders, orderId)).Text("rowVersion") }).EnsureOkAsync();
        order = await GetAsync(admin, Orders, orderId);
        Assert.Equal(("Closed", 20.52m, 10.26m), (order.Text("status"), order.Decimal("invoicedAmount"), order.Decimal("creditedAmount")));
        Assert.Equal((9m, 0m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));

        var exposure = await admin.GetJsonAsync($"{Customers}/{customerId}/exposure");
        Assert.True(exposure.GetProperty("isPartial").GetBoolean());
        Assert.Equal((0m, 20.52m, 10.26m), (exposure.Decimal("uninvoicedOrderAmount"), exposure.Decimal("invoicedAmount"),
            exposure.Decimal("pendingCreditAmount")));
        var events = order.GetProperty("events").EnumerateArray().Select(e => e.Text("action")).ToList();
        Assert.Equal(["Created", "Confirmed", "Delivered", "Invoiced", "Returned", "Closed"], events);
        await AssertReservationsReconcileAsync(admin, room);
    }

    [Fact]
    public async Task An_order_above_the_discount_threshold_waits_for_an_approver_who_may_be_its_creator_within_scope()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin);
        var settings = await admin.GetJsonAsync(Settings);
        var rowVersion = await admin.PutJsonAsync(Settings, SettingsBody(settings, discountApprovalThreshold: 5m)).ReadUpdatedAsync();
        try
        {
            var rep = User(room.WarehouseId, SalesPermissions.OrdersManage, SalesPermissions.DocumentsRead, SalesPermissions.DocumentsApprove);
            var orderId = await DraftOrderAsync(rep, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 2m, discountPercent: 10m))
                .CreatedIdAsync();
            await ActAsync(rep, Orders, orderId, "submit").EnsureOkAsync();
            var submitted = await GetAsync(rep, Orders, orderId);
            Assert.Equal(("Submitted", "DiscountAboveThreshold"),
                (submitted.Text("status"), submitted.GetProperty("approvalReasons")[0].GetString()));
            Assert.Equal((10m, 0m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));

            var clerk = User(room.WarehouseId, SalesPermissions.OrdersManage, SalesPermissions.DocumentsRead);
            await (await ActAsync(clerk, Orders, orderId, "approve")).AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
            var otherWarehouse = await InventoryTesting.CreateWarehouseAsync(admin, room.BranchId);
            var outsider = User(otherWarehouse, SalesPermissions.DocumentsApprove, SalesPermissions.DocumentsRead);
            await (await ActAsync(outsider, Orders, orderId, "approve", rowVersion: submitted.Text("rowVersion")))
                .AssertProblemAsync(HttpStatusCode.Forbidden, "Sales.Access.WarehouseDenied");

            await ActAsync(rep, Orders, orderId, "approve").EnsureOkAsync(); // its creator approves it
            var confirmed = await GetAsync(rep, Orders, orderId);
            Assert.Equal("Confirmed", confirmed.Text("status"));
            Assert.Equal(confirmed.Text("createdBy"), confirmed.Text("approvedBy"));
            Assert.Equal((10m, 2m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));
        }
        finally
        {
            await admin.PutJsonAsync(Settings, SettingsBody(settings, discountApprovalThreshold: null, rowVersion)).EnsureOkAsync();
        }
    }

    [Fact]
    public async Task A_line_takes_the_list_price_and_only_a_price_manager_sets_another_or_one_the_list_lacks()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin);
        var rep = User(room.WarehouseId, SalesPermissions.OrdersManage, SalesPermissions.DocumentsRead);

        await (await DraftOrderAsync(rep, customerId, room.WarehouseId, Line(room.ProductId, room.Carton, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Line.PriceMissing");
        await (await DraftOrderAsync(rep, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 1m, unitPrice: 8m)))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Sales.Line.ManualPriceNotAllowed");
        await (await DraftOrderAsync(rep, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 1m, unitPrice: 0m)))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        await (await DraftOrderAsync(rep, customerId, room.WarehouseId,
                Line(room.ProductId, room.Piece, 1m, taxTreatment: SalesTaxTreatment.Exempt, taxRate: 14m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Line.TaxRateNotAllowed");

        var exempt = await DraftOrderAsync(rep, customerId, room.WarehouseId,
            Line(room.ProductId, room.Piece, 2m, taxTreatment: SalesTaxTreatment.Exempt)).CreatedIdAsync();
        Assert.Equal((20m, 0m), ((await GetAsync(rep, Orders, exempt)).Decimal("netAmount"), (await GetAsync(rep, Orders, exempt)).Decimal("taxAmount")));

        var pricer = User(room.WarehouseId, SalesPermissions.OrdersManage, SalesPermissions.DocumentsRead, SalesPermissions.PricesManage);
        var manual = await DraftOrderAsync(pricer, customerId, room.WarehouseId,
            Line(room.ProductId, room.Carton, 1m, unitPrice: 100m, taxRate: 5m)).CreatedIdAsync();
        var line = (await GetAsync(pricer, Orders, manual)).GetProperty("lines")[0];
        Assert.Equal(("Manual", JsonValueKind.Null, 5m, 5m), (line.Text("priceSource"), line.GetProperty("listUnitPrice").ValueKind,
            line.Decimal("taxRate"), line.Decimal("taxAmount")));
    }

    [Fact]
    public async Task A_credit_hold_blocks_confirmation_and_deliveries_and_the_exposure_is_only_informational()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin);
        var (orderId, lines) = await ConfirmedOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 3m));
        var draftId = await DraftOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 1m)).CreatedIdAsync();
        var delivery = await DraftDeliveryAsync(admin, orderId, (lines[0], 1m, null)).CreatedIdAsync();

        var customer = await GetAsync(admin, Customers, customerId);
        var noLimitReason = await admin.PutJsonAsync($"{Customers}/{customerId}/credit", new
        {
            paymentTermDays = 30, creditLimit = 1m, isOnCreditHold = true, creditHoldReason = (string?)null, rowVersion = customer.Text("rowVersion"),
        });
        await noLimitReason.AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        await admin.PutJsonAsync($"{Customers}/{customerId}/credit", new
        {
            paymentTermDays = 30, creditLimit = 1m, isOnCreditHold = true, creditHoldReason = "شيكات مرتدة", rowVersion = customer.Text("rowVersion"),
        }).EnsureOkAsync();

        await (await ActAsync(admin, Orders, draftId, "submit")).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Customer.OnCreditHold");
        await (await ActAsync(admin, Deliveries, delivery, "post", idempotencyKey: Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Customer.OnCreditHold");
        var exposure = await admin.GetJsonAsync($"{Customers}/{customerId}/exposure");
        Assert.True(exposure.GetProperty("isPartial").GetBoolean());
        Assert.Equal((1m, 34.2m, 1), (exposure.Decimal("creditLimit"), exposure.Decimal("uninvoicedOrderAmount"),
            exposure.GetProperty("openOrderCount").GetInt32()));
        var held = await GetAsync(admin, Customers, customerId);
        Assert.Equal(("شيكات مرتدة", 30), (held.Text("creditHoldReason"), held.GetProperty("paymentTermDays").GetInt32()));
        Assert.NotNull(held.Text("creditChangedAtUtc"));

        // The limit is informational: lifted hold, an order far above the limit still confirms.
        await admin.PutJsonAsync($"{Customers}/{customerId}/credit", new
        {
            paymentTermDays = 30, creditLimit = 1m, isOnCreditHold = false, creditHoldReason = (string?)null, rowVersion = held.Text("rowVersion"),
        }).EnsureOkAsync();
        await ActAsync(admin, Orders, draftId, "submit").EnsureOkAsync();
    }

    [Fact]
    public async Task Customers_have_unique_codes_and_registrations_and_warn_about_likely_duplicates()
    {
        var admin = Admin();
        var code = DatabaseFixture.Code("C");
        object Body(string customerCode, string type, string? tax = null, string? registration = null, string? registry = null,
            string name = "شركة النور للتجارة", string? mobile = null) => new
        {
            code = customerCode, customerType = type, nameAr = name, nameEn = (string?)null, countryCode = "EG", taxRegistrationNumber = tax,
            commercialRegistrationNumber = registration, commercialRegistry = registry, phone = (string?)null, mobile, email = (string?)null,
            notes = (string?)null,
        };
        var tax = Random.Shared.Next(100_000_000, 999_999_999).ToString(System.Globalization.CultureInfo.InvariantCulture);

        var first = await admin.PostJsonAsync(Customers, Body(code, "Business", tax, mobile: "0100 123 4567")).CreatedIdAsync();
        await (await admin.PostJsonAsync(Customers, Body(code, "Business"))).AssertProblemAsync(HttpStatusCode.Conflict, "Sales.Customer.CodeTaken");
        await (await admin.PostJsonAsync(Customers, Body(DatabaseFixture.Code("C"), "Business", $"{tax[..3]}-{tax[3..6]}-{tax[6..]}")))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Sales.Customer.TaxNumberTaken");
        await (await admin.PostJsonAsync(Customers, Body(DatabaseFixture.Code("C"), "Business", "12345")))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Customer.EgyptianTaxNumberInvalid");
        await (await admin.PostJsonAsync(Customers, Body(DatabaseFixture.Code("C"), "Individual", registration: "CR-1", registry: "القاهرة")))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Customer.CommercialRegistrationForBusinessOnly");

        var similar = await admin.GetJsonAsync($"{Customers}/similar?nameAr={Uri.EscapeDataString("النور للتجاره")}&mobile=01001234567");
        Assert.Contains(similar.EnumerateArray(), s => s.GetProperty("id").GetGuid() == first);
        var listed = await admin.GetJsonAsync($"{Customers}?search=01001234567&customerType=Business");
        Assert.Contains(listed.GetProperty("items").EnumerateArray(), s => s.GetProperty("id").GetGuid() == first);

        await admin.PostAsync($"{Customers}/{first}/deactivate", null).AssertNoContentAsync();
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        await SetPriceAsync(admin, room.Piece, 5m);
        await (await DraftOrderAsync(admin, first, room.WarehouseId, Line(room.ProductId, room.Piece, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Customer.NotActive");
    }

    [Fact]
    public async Task Returns_take_back_only_goods_fit_for_resale_and_never_more_than_was_delivered()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin);
        var (orderId, lines) = await ConfirmedOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 3m));
        var deliveryId = await DeliverAsync(admin, orderId, (lines[0], 3m, null));
        var deliveryLine = (await GetAsync(admin, Deliveries, deliveryId)).GetProperty("lines")[0].GetProperty("id").GetGuid();

        await (await DraftReturnAsync(admin, deliveryId, (deliveryLine, 1m, ReturnCondition.Damaged)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Return.ConditionNotSupported");
        await (await DraftReturnAsync(admin, deliveryId, (deliveryLine, 4m, ReturnCondition.FitForResale)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Delivery.ReturnExceedsDelivery");

        var first = await DraftReturnAsync(admin, deliveryId, (deliveryLine, 2m, ReturnCondition.FitForResale)).CreatedIdAsync();
        var second = await DraftReturnAsync(admin, deliveryId, (deliveryLine, 2m, ReturnCondition.FitForResale)).CreatedIdAsync();
        await ActAsync(admin, Returns, first, "submit").EnsureOkAsync();
        await ActAsync(admin, Returns, second, "submit").EnsureOkAsync();
        var clerk = User(room.WarehouseId, SalesPermissions.ReturnsManage, SalesPermissions.DocumentsRead);
        await (await ActAsync(clerk, Returns, first, "approve", idempotencyKey: Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
        await ActAsync(admin, Returns, first, "approve", idempotencyKey: Guid.NewGuid()).EnsureOkAsync();
        await (await ActAsync(admin, Returns, second, "approve", idempotencyKey: Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Sales.Delivery.ReturnExceedsDelivery");

        Assert.Equal((9m, 0m), await AvailabilityAsync(admin, room.WarehouseId, room.ProductId));
        await InventoryTesting.AssertValuedAsync(admin, room.ProductId, 9m, 54m);
        var line = (await GetAsync(admin, Orders, orderId)).GetProperty("lines")[0];
        Assert.Equal((2m, 0m, 1m), (line.Decimal("returnedQuantity"), line.Decimal("returnedInvoicedQuantity"), line.Decimal("invoiceableQuantity")));
    }

    [Fact]
    public async Task Inventory_reservations_and_Sales_documents_stay_in_their_warehouse_scope()
    {
        var admin = Admin();
        var (room, customerId) = await SetUpAsync(admin);
        var (orderId, _) = await ConfirmedOrderAsync(admin, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 2m));
        var otherWarehouse = await InventoryTesting.CreateWarehouseAsync(admin, room.BranchId);
        var outsider = User(otherWarehouse, SalesPermissions.DocumentsRead, SalesPermissions.OrdersManage, SalesPermissions.DeliveriesManage);

        await (await outsider.GetAsync($"{Orders}/{orderId}")).AssertProblemAsync(HttpStatusCode.Forbidden, "Sales.Access.WarehouseDenied");
        await (await DraftOrderAsync(outsider, customerId, room.WarehouseId, Line(room.ProductId, room.Piece, 1m)))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Sales.Access.WarehouseDenied");
        await (await DraftDeliveryAsync(outsider, orderId, (Guid.NewGuid(), 1m, null)))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Sales.Access.WarehouseDenied");
        var listed = await outsider.GetJsonAsync($"{Orders}?customerId={customerId}");
        Assert.Empty(listed.GetProperty("items").EnumerateArray());

        var reservations = await admin.GetJsonAsync($"/api/inventory/reservations?warehouseId={room.WarehouseId}");
        var reservation = reservations.GetProperty("items").EnumerateArray().Single();
        Assert.Equal((orderId, 2m), (reservation.GetProperty("sourceDocumentId").GetGuid(), reservation.Decimal("remainingQuantity")));
    }

    private static object SettingsBody(JsonElement current, decimal? discountApprovalThreshold, string? rowVersion = null) => new
    {
        quotationsRequireApproval = current.GetProperty("quotationsRequireApproval").GetBoolean(),
        ordersRequireApproval = current.GetProperty("ordersRequireApproval").GetBoolean(),
        discountApprovalThreshold,
        manualPriceRequiresApproval = current.GetProperty("manualPriceRequiresApproval").GetBoolean(),
        priceBelowListRequiresApproval = current.GetProperty("priceBelowListRequiresApproval").GetBoolean(),
        creditHoldPolicy = current.Text("creditHoldPolicy"),
        creditHoldBlocksDeliveries = current.GetProperty("creditHoldBlocksDeliveries").GetBoolean(),
        defaultVatRate = current.Decimal("defaultVatRate"),
        quotationValidityDays = current.GetProperty("quotationValidityDays").GetInt32(),
        rowVersion = rowVersion ?? current.Text("rowVersion"),
    };

    // The warehouse's reserved quantity is what its reservations hold, and reconciliation reports no difference for it.
    private static async Task AssertReservationsReconcileAsync(HttpClient admin, Stockroom room)
    {
        var report = await admin.GetJsonAsync("/api/inventory/reconciliation");
        Assert.DoesNotContain(report.GetProperty("reservationDifferences").EnumerateArray(), d => d.GetProperty("productId").GetGuid() == room.ProductId);
        Assert.DoesNotContain(report.GetProperty("availabilityDifferences").EnumerateArray(), d => d.GetProperty("productId").GetGuid() == room.ProductId);
        Assert.DoesNotContain(report.GetProperty("valuationDifferences").EnumerateArray(), d => d.GetProperty("productId").GetGuid() == room.ProductId);
    }
}
