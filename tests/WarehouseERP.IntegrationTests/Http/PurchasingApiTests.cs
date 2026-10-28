using System.Net;
using System.Text.Json;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Purchasing.Contracts;
using static WarehouseERP.IntegrationTests.Http.PurchasingTesting;

namespace WarehouseERP.IntegrationTests.Http;

// Purchasing through the real host on SQL Server: an order received in parts and posted to stock through Inventory,
// matched invoices with price variances, approved returns that leave stock and claim credits, requests that orders draw
// on, and the rules that keep it consistent under retries, races and scopes. Approvers may approve their own documents.
[Collection(DatabaseCollection.Name)]
public sealed class PurchasingApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(PurchasingTesting.AllPermissions);

    private HttpClient Scoped(Guid warehouseId, params string[] permissions) =>
        _api.CreateClient().As(string.Join(',', permissions), warehouseId.ToString(), Guid.NewGuid());

    private static string Status(JsonElement document) => document.GetProperty("status").GetString()!;

    [Fact]
    public async Task An_order_is_received_in_parts_into_stock_at_its_net_price_and_a_retry_never_receives_twice()
    {
        var admin = Admin();
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        var supplier = await CreateSupplierAsync(admin);
        var (orderId, lines) = await ApprovedOrderAsync(admin, supplier, room.WarehouseId,
            OrderLine(room.ProductId, room.Carton, 5m, 120m, discountPercent: 10m, taxRate: 14m));
        var order = await GetAsync(admin, Orders, orderId);
        Assert.Equal(("Approved", 600m, 60m, 540m, 75.60m, 615.60m), (Status(order), order.GetProperty("grossAmount").GetDecimal(),
            order.GetProperty("discountAmount").GetDecimal(), order.GetProperty("netAmount").GetDecimal(),
            order.GetProperty("taxAmount").GetDecimal(), order.GetProperty("totalAmount").GetDecimal()));
        Assert.StartsWith("PO-B-", order.GetProperty("number").GetString());

        var key = Guid.NewGuid();
        var first = await ReceiveAsync(admin, orderId, key, (lines[0], 2m)).PostedAsync();
        var retry = await ReceiveAsync(admin, orderId, key, (lines[0], 2m)).PostedAsync();
        Assert.Equal(first.GetProperty("id").GetGuid(), retry.GetProperty("id").GetGuid());
        await (await ReceiveAsync(admin, orderId, key, (lines[0], 1m))).AssertProblemAsync(HttpStatusCode.Conflict, "Purchasing.Idempotency.KeyReused");
        await (await ReceiveAsync(admin, orderId, Guid.NewGuid(), (lines[0], 3.5m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.PurchaseOrder.OverReceipt");

        // 2 cartons of 12 at 108 a carton (120 less 10 %): 24 pieces worth 216, recoverable tax left out.
        Assert.Equal(24m, await InventoryTesting.BalanceAsync(admin, room));
        await InventoryTesting.AssertValuedAsync(admin, room.ProductId, 24m, 216m);
        var stockDocument = await InventoryTesting.DocumentAsync(admin, first.GetProperty("stockDocumentId").GetGuid());
        Assert.Equal(("PurchaseReceipt", "Posted", first.GetProperty("id").GetGuid(), first.GetProperty("number").GetString()),
            (stockDocument.GetProperty("documentType").GetString(), Status(stockDocument), stockDocument.GetProperty("sourceDocumentId").GetGuid(),
                stockDocument.GetProperty("externalReference").GetString()));
        Assert.StartsWith("PRC-B-", first.GetProperty("stockDocumentNumber").GetString());
        await (await admin.PostJsonAsync($"{InventoryTesting.Documents}/{stockDocument.GetProperty("id").GetGuid()}/reverse", new { notes = "x" }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.ManagedByPurchasing");

        await ReceiveAsync(admin, orderId, Guid.NewGuid(), (lines[0], 3m)).PostedAsync();
        order = await GetAsync(admin, Orders, orderId);
        Assert.Equal(("Received", 5m, 0m), (Status(order), order.GetProperty("lines")[0].GetProperty("receivedQuantity").GetDecimal(),
            order.GetProperty("lines")[0].GetProperty("remainingQuantity").GetDecimal()));
        Assert.Equal(2, order.GetProperty("documents").GetArrayLength());
        Assert.Equal(["Created", "Submitted", "Approved", "GoodsReceived", "GoodsReceived"],
            order.GetProperty("events").EnumerateArray().Select(e => e.GetProperty("action").GetString()));
    }

    [Fact]
    public async Task Two_receipts_racing_for_what_remains_never_over_receive()
    {
        var admin = Admin();
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        var supplier = await CreateSupplierAsync(admin);
        var (orderId, lines) = await ApprovedOrderAsync(admin, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 10m, 5m));
        await ReceiveAsync(admin, orderId, Guid.NewGuid(), (lines[0], 4m)).PostedAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => ReceiveAsync(_api.CreateClient().As(PurchasingTesting.AllPermissions),
            orderId, Guid.NewGuid(), (lines[0], 6m))));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        var refused = Assert.Single(responses, r => r.StatusCode != HttpStatusCode.OK);
        await refused.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.PurchaseOrder.NotReceivable");
        Assert.Equal(10m, await InventoryTesting.BalanceAsync(admin, room));
        Assert.Equal(10m, (await GetAsync(admin, Orders, orderId)).GetProperty("lines")[0].GetProperty("receivedQuantity").GetDecimal());
    }

    [Fact]
    public async Task Invoices_bill_only_what_was_received_once_and_a_price_difference_waits_for_approval_without_revaluing_stock()
    {
        var admin = Admin();
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        var supplier = await CreateSupplierAsync(admin);
        var (orderId, lines) = await ApprovedOrderAsync(admin, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 10m, 10m, taxRate: 14m));
        await ReceiveAsync(admin, orderId, Guid.NewGuid(), (lines[0], 8m)).PostedAsync();

        await (await DraftInvoiceAsync(admin, orderId, "INV-1", InvoiceLine(lines[0], 9m, 10m, 14m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.PurchaseOrder.InvoiceExceedsReceipts");
        var exact = await DraftInvoiceAsync(admin, orderId, "INV-0001", InvoiceLine(lines[0], 5m, 10m, 14m)).CreatedIdAsync();
        var draft = await GetAsync(admin, Invoices, exact);
        Assert.Equal((false, 50m, 7m, 57m, new DateOnly(2026, 2, 14)), (draft.GetProperty("requiresApproval").GetBoolean(),
            draft.GetProperty("netAmount").GetDecimal(), draft.GetProperty("taxAmount").GetDecimal(), draft.GetProperty("totalAmount").GetDecimal(),
            DateOnly.Parse(draft.GetProperty("dueDate").GetString()!, System.Globalization.CultureInfo.InvariantCulture)));
        await (await DraftInvoiceAsync(admin, orderId, "inv 0001", InvoiceLine(lines[0], 1m, 10m)))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Purchasing.SupplierInvoice.InvoiceNumberTaken");
        var key = Guid.NewGuid();
        var posted = await ActAsync(admin, Invoices, exact, "post", idempotencyKey: key).PostedAsync();
        Assert.StartsWith("SIN-B-", posted.GetProperty("number").GetString());
        await ActAsync(admin, Invoices, exact, "post", idempotencyKey: key, rowVersion: draft.GetProperty("rowVersion").GetString()).PostedAsync();

        var dearer = await DraftInvoiceAsync(admin, orderId, "INV-2", InvoiceLine(lines[0], 3m, 11m, 14m)).CreatedIdAsync();
        await (await ActAsync(admin, Invoices, dearer, "post", idempotencyKey: Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.SupplierInvoice.ApprovalRequired");
        await ActAsync(admin, Invoices, dearer, "submit").EnsureOkAsync();
        var approved = await ActAsync(admin, Invoices, dearer, "approve", idempotencyKey: Guid.NewGuid()).PostedAsync();
        Assert.Equal(3m, approved.GetProperty("priceVarianceAmount").GetDecimal());
        var invoice = await GetAsync(admin, Invoices, dearer);
        Assert.Equal(("Posted", 3m), (Status(invoice), invoice.GetProperty("lines")[0].GetProperty("priceVarianceAmount").GetDecimal()));

        var line = (await GetAsync(admin, Orders, orderId)).GetProperty("lines")[0];
        Assert.Equal((8m, 0m), (line.GetProperty("invoicedQuantity").GetDecimal(), line.GetProperty("invoiceableQuantity").GetDecimal()));
        await InventoryTesting.AssertValuedAsync(admin, room.ProductId, 8m, 80m); // provisional cost stays the order's price
    }

    [Fact]
    public async Task An_approved_return_leaves_stock_at_the_average_and_claims_a_credit_only_for_what_was_already_invoiced()
    {
        var admin = Admin();
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        var supplier = await CreateSupplierAsync(admin);
        var (orderId, lines) = await ApprovedOrderAsync(admin, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 10m, 10m, taxRate: 14m));
        var receipt = await ReceiveAsync(admin, orderId, Guid.NewGuid(), (lines[0], 10m)).PostedAsync();
        var receiptId = receipt.GetProperty("id").GetGuid();
        var invoice = await DraftInvoiceAsync(admin, orderId, "INV-R", InvoiceLine(lines[0], 6m, 10m, 14m)).CreatedIdAsync();
        await ActAsync(admin, Invoices, invoice, "post", idempotencyKey: Guid.NewGuid()).PostedAsync();
        var receiptLine = (await GetAsync(admin, Receipts, receiptId)).GetProperty("lines")[0].GetProperty("id").GetGuid();

        await (await DraftReturnAsync(admin, receiptId, (receiptLine, 11m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.SupplierReturn.ExceedsReceipt");
        var returnId = await DraftReturnAsync(admin, receiptId, (receiptLine, 5m)).CreatedIdAsync();
        await (await ActAsync(admin, Returns, returnId, "approve", idempotencyKey: Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.SupplierReturn.NotSubmitted");
        await ActAsync(admin, Returns, returnId, "submit").EnsureOkAsync();
        var key = Guid.NewGuid();
        var submitted = (await GetAsync(admin, Returns, returnId)).GetProperty("rowVersion").GetString();
        var posted = await ActAsync(admin, Returns, returnId, "approve", idempotencyKey: key, rowVersion: submitted).PostedAsync(); // its creator approves
        var replayed = await ActAsync(admin, Returns, returnId, "approve", idempotencyKey: key, rowVersion: submitted).PostedAsync();
        Assert.Equal(posted.GetProperty("number").GetString(), replayed.GetProperty("number").GetString());
        Assert.StartsWith("PRT-B-", posted.GetProperty("stockDocumentNumber").GetString());

        // 4 not invoiced yet go back without a claim; 1 already invoiced at 10 + 14 % tax is claimed.
        Assert.Equal(11.40m, posted.GetProperty("creditClaimTotalAmount").GetDecimal());
        var line = (await GetAsync(admin, Returns, returnId)).GetProperty("lines")[0];
        Assert.Equal((4m, 1m, 10m, 1.40m), (line.GetProperty("uninvoicedQuantity").GetDecimal(), line.GetProperty("invoicedQuantity").GetDecimal(),
            line.GetProperty("creditClaimNetAmount").GetDecimal(), line.GetProperty("creditClaimTaxAmount").GetDecimal()));
        Assert.Equal(5m, await InventoryTesting.BalanceAsync(admin, room));
        await InventoryTesting.AssertValuedAsync(admin, room.ProductId, 5m, 50m);
        var stockDocument = await InventoryTesting.DocumentAsync(admin, posted.GetProperty("stockDocumentId").GetGuid());
        Assert.Equal(("PurchaseReturn", returnId), (stockDocument.GetProperty("documentType").GetString(), stockDocument.GetProperty("sourceDocumentId").GetGuid()));

        var order = await GetAsync(admin, Orders, orderId);
        Assert.Equal(("Received", 0m), (Status(order), order.GetProperty("lines")[0].GetProperty("invoiceableQuantity").GetDecimal()));
        var claims = await admin.GetJsonAsync($"{Returns}?purchaseOrderId={orderId}&withCreditClaim=true");
        Assert.Equal(1, claims.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task A_return_of_goods_no_longer_in_stock_is_refused_by_inventory_and_changes_nothing()
    {
        var admin = Admin();
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        var supplier = await CreateSupplierAsync(admin);
        var (orderId, lines) = await ApprovedOrderAsync(admin, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 10m, 10m));
        var receiptId = (await ReceiveAsync(admin, orderId, Guid.NewGuid(), (lines[0], 10m)).PostedAsync()).GetProperty("id").GetGuid();
        await InventoryTesting.IssueAsync(admin, room, 8m).PostedAsync();
        var receiptLine = (await GetAsync(admin, Receipts, receiptId)).GetProperty("lines")[0].GetProperty("id").GetGuid();
        var returnId = await DraftReturnAsync(admin, receiptId, (receiptLine, 5m)).CreatedIdAsync();
        await ActAsync(admin, Returns, returnId, "submit").EnsureOkAsync();

        await (await ActAsync(admin, Returns, returnId, "approve", idempotencyKey: Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
        Assert.Equal("Submitted", Status(await GetAsync(admin, Returns, returnId)));
        Assert.Equal(0m, (await GetAsync(admin, Receipts, receiptId)).GetProperty("lines")[0].GetProperty("returnedQuantity").GetDecimal());
        Assert.Equal(2m, await InventoryTesting.BalanceAsync(admin, room));
    }

    [Fact]
    public async Task Orders_draw_on_approved_requests_never_beyond_them_and_a_cancelled_order_gives_its_quantity_back()
    {
        var admin = Admin();
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        var supplier = await CreateSupplierAsync(admin);
        var requestId = await admin.PostJsonAsync(Requests, new
        {
            warehouseId = room.WarehouseId, neededByDate = (DateOnly?)null, notes = (string?)null,
            lines = new[] { new { productId = room.ProductId, productUnitId = room.Piece, quantity = 10m, notes = (string?)null } },
        }).CreatedIdAsync();
        await ActAsync(admin, Requests, requestId, "submit").EnsureOkAsync();
        await ActAsync(admin, Requests, requestId, "approve").EnsureOkAsync(); // its creator approves
        var requestLine = (await GetAsync(admin, Requests, requestId)).GetProperty("lines")[0].GetProperty("id").GetGuid();

        await (await DraftOrderAsync(admin, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 11m, 5m, purchaseRequestLineId: requestLine)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.PurchaseRequest.ExceedsRequest");
        await (await DraftOrderAsync(admin, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Carton, 1m, 5m, purchaseRequestLineId: requestLine)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.PurchaseRequest.ProductMismatch");
        var first = await DraftOrderAsync(admin, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 6m, 5m, purchaseRequestLineId: requestLine))
            .CreatedIdAsync();
        await (await DraftOrderAsync(admin, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 5m, 5m, purchaseRequestLineId: requestLine)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.PurchaseRequest.ExceedsRequest");
        var second = await DraftOrderAsync(admin, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 4m, 5m, purchaseRequestLineId: requestLine))
            .CreatedIdAsync();
        var request = await GetAsync(admin, Requests, requestId);
        Assert.Equal(("Ordered", 10m), (Status(request), request.GetProperty("lines")[0].GetProperty("orderedQuantity").GetDecimal()));

        await ActAsync(admin, Orders, first, "cancel", body: new { reason = "supplier out of stock",
            rowVersion = (await GetAsync(admin, Orders, first)).GetProperty("rowVersion").GetString() }).EnsureOkAsync();
        request = await GetAsync(admin, Requests, requestId);
        Assert.Equal(("Approved", 4m, 6m), (Status(request), request.GetProperty("lines")[0].GetProperty("orderedQuantity").GetDecimal(),
            request.GetProperty("lines")[0].GetProperty("remainingQuantity").GetDecimal()));
        Assert.Equal("Draft", Status(await GetAsync(admin, Orders, second)));
    }

    [Fact]
    public async Task Approval_follows_the_permission_and_scope_and_whoever_approves_is_recorded_creator_included()
    {
        var admin = Admin();
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        var supplier = await CreateSupplierAsync(admin);
        var buyerId = Guid.NewGuid();
        var buyer = _api.CreateClient().As(string.Join(',', PurchasingPermissions.OrdersManage, PurchasingPermissions.DocumentsRead,
            PurchasingPermissions.DocumentsApprove), room.WarehouseId.ToString(), buyerId);
        var clerk = Scoped(room.WarehouseId, PurchasingPermissions.OrdersManage, PurchasingPermissions.DocumentsRead);
        var outsider = Scoped(Guid.NewGuid(), PurchasingPermissions.DocumentsApprove, PurchasingPermissions.DocumentsRead);

        var orderId = await DraftOrderAsync(buyer, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 1m, 1m)).CreatedIdAsync();
        await ActAsync(buyer, Orders, orderId, "submit").EnsureOkAsync();
        var rowVersion = (await GetAsync(buyer, Orders, orderId)).GetProperty("rowVersion").GetString();
        await (await ActAsync(clerk, Orders, orderId, "approve", rowVersion: rowVersion)).AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
        await (await ActAsync(outsider, Orders, orderId, "approve", rowVersion: rowVersion))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Purchasing.Access.WarehouseDenied");
        await (await ActAsync(buyer, Orders, orderId, "submit", rowVersion: rowVersion))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.PurchaseOrder.NotDraft");

        await ActAsync(buyer, Orders, orderId, "approve", rowVersion: rowVersion).EnsureOkAsync();
        var order = await GetAsync(buyer, Orders, orderId);
        Assert.Equal(("Approved", buyerId), (Status(order), order.GetProperty("createdByUserId").GetGuid()));
        Assert.Equal(buyerId, order.GetProperty("events").EnumerateArray().Single(e => e.GetProperty("action").GetString() == "Approved")
            .GetProperty("userId").GetGuid());
        await (await outsider.GetAsync($"{Orders}/{orderId}")).AssertProblemAsync(HttpStatusCode.Forbidden, "Purchasing.Access.WarehouseDenied");
    }

    [Fact]
    public async Task Suppliers_are_unique_by_code_and_registration_similar_names_only_warn_and_inactive_ones_take_no_new_orders()
    {
        var admin = Admin();
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        var taxNumber = Random.Shared.Next(100_000_000, 999_999_999).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var supplier = await CreateSupplierAsync(admin, taxNumber, "شركة النيل للتوريدات");
        await (await admin.PostJsonAsync(Suppliers, new
        {
            code = DatabaseFixture.Code("S"), nameAr = "آخر", countryCode = "EG", taxRegistrationNumber = $"{taxNumber[..3]}-{taxNumber[3..6]}-{taxNumber[6..]}",
            paymentTermDays = 0,
        })).AssertProblemAsync(HttpStatusCode.Conflict, "Purchasing.Supplier.TaxNumberTaken");
        await (await admin.PostJsonAsync(Suppliers, new { code = "X", nameAr = "مورد", countryCode = "EG", taxRegistrationNumber = "1234", paymentTermDays = 0 }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.Supplier.EgyptianTaxNumberInvalid");

        var similar = await admin.GetJsonAsync($"{Suppliers}/similar?nameAr={Uri.EscapeDataString("النيل للتوريدات")}");
        Assert.Contains(supplier, similar.EnumerateArray().Select(s => s.GetProperty("id").GetGuid()));

        var (orderId, lines) = await ApprovedOrderAsync(admin, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 2m, 1m));
        (await admin.PostAsync($"{Suppliers}/{supplier}/deactivate", null)).EnsureSuccessStatusCode();
        await (await DraftOrderAsync(admin, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 1m, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.Supplier.NotActive");
        await ReceiveAsync(admin, orderId, Guid.NewGuid(), (lines[0], 2m)).PostedAsync(); // an approved order is still received

        var (tracked, trackedUnit) = await InventoryTesting.CreateProductAsync(admin, (await fx.SeedBranchAndUnitAsync()).UnitId, tracking: TrackingMode.Batch);
        var other = await CreateSupplierAsync(admin);
        await (await DraftOrderAsync(admin, other, room.WarehouseId, OrderLine(tracked, trackedUnit, 1m, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.Reference.TrackingNotSupported");
        await (await DraftOrderAsync(admin, other, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 1m, 1m, taxRate: 14m, taxTreatment: "NonRecoverable")))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Purchasing.Line.NonRecoverableTaxNotSupported");
    }

    [Fact]
    public async Task Without_an_approval_policy_a_submitted_order_is_approved_at_once_and_inventory_types_stay_purchasing_only()
    {
        var admin = Admin();
        var room = await InventoryTesting.CreateStockroomAsync(fx, admin);
        var supplier = await CreateSupplierAsync(admin);
        var policy = (await admin.GetJsonAsync(Policies)).EnumerateArray().Single(p => p.GetProperty("documentType").GetString() == "PurchaseOrder");
        var changed = await admin.PutJsonAsync($"{Policies}/PurchaseOrder", new { requiresApproval = false, rowVersion = policy.GetProperty("rowVersion").GetString() })
            .ReadUpdatedAsync();
        try
        {
            var orderId = await DraftOrderAsync(admin, supplier, room.WarehouseId, OrderLine(room.ProductId, room.Piece, 1m, 1m)).CreatedIdAsync();
            await ActAsync(admin, Orders, orderId, "submit").EnsureOkAsync();
            var order = await GetAsync(admin, Orders, orderId);
            Assert.Equal(("Approved", false), (Status(order), order.GetProperty("requiresApproval").GetBoolean()));
        }
        finally
        {
            await admin.PutJsonAsync($"{Policies}/PurchaseOrder", new { requiresApproval = true, rowVersion = changed }).ReadUpdatedAsync();
        }

        await (await InventoryTesting.DraftAsync(admin, StockDocumentType.PurchaseReceipt, room.WarehouseId, null,
                InventoryTesting.Line(room.ProductId, room.Piece, 1m, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.ManagedByPurchasing");
    }
}
