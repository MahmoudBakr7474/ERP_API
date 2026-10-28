using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using static WarehouseERP.IntegrationTests.Http.InventoryTesting;

namespace WarehouseERP.IntegrationTests.Http;

// Stock documents through the real host on SQL Server: drafting, posting and approval, numbering, the ledger,
// availability, valuation, reversals and the scope rules.
[Collection(DatabaseCollection.Name)]
public sealed class InventoryDocumentApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(AllPermissions);

    private HttpClient OtherAdmin() => _api.CreateClient().As(AllPermissions, userId: Approver);

    [Fact]
    public async Task A_receipt_posts_directly_and_updates_ledger_balance_availability_and_valuation()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);

        var id = await DraftIdAsync(client, StockDocumentType.Receipt, room.WarehouseId, null,
            Line(room.ProductId, room.Carton, 2m, unitCost: 120m));
        var draft = await DocumentAsync(client, id);
        Assert.Equal(("Draft", false, JsonValueKind.Null), (draft.GetProperty("status").GetString(),
            draft.GetProperty("requiresApproval").GetBoolean(), draft.GetProperty("number").ValueKind));
        Assert.Matches("^DR-[0-9]{6,}$", draft.GetProperty("draftReference").GetString());
        var line = draft.GetProperty("lines")[0];
        Assert.Equal((2m, 24m, 12m, 120m, room.DefaultLocationId, "DEFAULT"),
            (line.GetProperty("quantity").GetDecimal(), line.GetProperty("baseQuantity").GetDecimal(),
                line.GetProperty("conversionFactor").GetDecimal(), line.GetProperty("unitCost").GetDecimal(),
                line.GetProperty("locationId").GetGuid(), line.GetProperty("locationCode").GetString()));

        var posted = await ActAsync(client, id, "post").PostedAsync();

        var number = posted.GetProperty("number").GetString()!;
        Assert.StartsWith("RCV-B-", number);
        Assert.EndsWith("-2026-000001", number);
        Assert.Equal(0, posted.GetProperty("overReservations").GetArrayLength());
        var document = await DocumentAsync(client, id);
        Assert.Equal(("Posted", number), (document.GetProperty("status").GetString(), document.GetProperty("number").GetString()));
        Assert.Equal(["Created", "Posted"], document.GetProperty("events").EnumerateArray().Select(e => e.GetProperty("action").GetString()));

        Assert.Equal(24m, await BalanceAsync(client, room));
        var availability = (await client.GetJsonAsync($"/api/inventory/availability?warehouseId={room.WarehouseId}&productId={room.ProductId}"))
            .GetProperty("items")[0];
        Assert.Equal((24m, 0m, 24m), (availability.GetProperty("onHandSellable").GetDecimal(),
            availability.GetProperty("reservedQuantity").GetDecimal(), availability.GetProperty("available").GetDecimal()));
        var card = (await client.GetJsonAsync($"/api/inventory/movements?productId={room.ProductId}&warehouseId={room.WarehouseId}"))
            .GetProperty("items")[0];
        Assert.Equal((24m, 24m, number), (card.GetProperty("quantity").GetDecimal(), card.GetProperty("balanceAfter").GetDecimal(),
            card.GetProperty("documentNumber").GetString()));
        var valuation = await ValuationAsync(client, room.ProductId);
        Assert.Equal((24m, 240m, 10m), (valuation.GetProperty("totalQuantity").GetDecimal(),
            valuation.GetProperty("totalValue").GetDecimal(), valuation.GetProperty("averageUnitCost").GetDecimal()));
    }

    [Fact]
    public async Task An_issue_takes_stock_at_the_average_and_never_more_than_there_is()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        await ReceiveAsync(client, room, 10m, 5m);
        await ReceiveAsync(client, room, 10m, 7m);

        var issued = await IssueAsync(client, room, 4m).PostedAsync();
        Assert.EndsWith("-2026-000001", issued.GetProperty("number").GetString());
        await (await IssueAsync(client, room, 17m)).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");

        Assert.Equal(16m, await BalanceAsync(client, room));
        var valuation = await ValuationAsync(client, room.ProductId);
        Assert.Equal((16m, 96m, 6m), (valuation.GetProperty("totalQuantity").GetDecimal(),
            valuation.GetProperty("totalValue").GetDecimal(), valuation.GetProperty("averageUnitCost").GetDecimal()));
        var entries = (await client.GetJsonAsync($"/api/inventory/valuation/{room.ProductId}/entries")).GetProperty("items");
        Assert.Equal([-24m, 70m, 50m], entries.EnumerateArray().Select(e => e.GetProperty("valueDelta").GetDecimal()));
    }

    [Fact]
    public async Task Adjustments_wait_for_an_approver_who_did_not_create_them_and_a_rejection_sends_them_back()
    {
        var maker = Admin();
        var checker = OtherAdmin();
        var room = await CreateStockroomAsync(fx, maker);
        await ReceiveAsync(maker, room, 10m, 3m);
        var id = await DraftIdAsync(maker, StockDocumentType.Adjustment, room.WarehouseId, AdjustmentReason.Damage,
            Line(room.ProductId, room.Piece, 2m, direction: StockDirection.Out));

        await (await ActAsync(maker, id, "post")).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.ApprovalRequired");
        Assert.Equal(HttpStatusCode.OK, (await ActAsync(maker, id, "submit")).StatusCode);
        await (await ActAsync(maker, id, "approve")).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.SelfApproval");
        var rowVersion = (await DocumentAsync(checker, id)).GetProperty("rowVersion").GetString();
        Assert.Equal(HttpStatusCode.OK, (await ActAsync(checker, id, "reject", body: new { reason = "الكمية غير صحيحة", rowVersion })).StatusCode);
        Assert.Equal("Draft", (await DocumentAsync(maker, id)).GetProperty("status").GetString());

        Assert.Equal(HttpStatusCode.OK, (await ActAsync(maker, id, "submit")).StatusCode);
        var posted = await ActAsync(checker, id, "approve").PostedAsync();

        Assert.StartsWith("ADJ-", posted.GetProperty("number").GetString());
        var document = await DocumentAsync(maker, id);
        Assert.Equal(["Created", "Submitted", "Rejected", "Submitted", "Approved", "Posted"],
            document.GetProperty("events").EnumerateArray().Select(e => e.GetProperty("action").GetString()));
        Assert.Equal("الكمية غير صحيحة", document.GetProperty("events")[2].GetProperty("note").GetString());
        Assert.Equal(8m, await BalanceAsync(maker, room));
        Assert.Equal(24m, (await ValuationAsync(maker, room.ProductId)).GetProperty("totalValue").GetDecimal());
    }

    [Fact]
    public async Task Opening_stock_is_approved_and_closes_once_anything_else_posts_in_the_warehouse()
    {
        var maker = Admin();
        var checker = OtherAdmin();
        var room = await CreateStockroomAsync(fx, maker);

        foreach (var pieces in new[] { 5m, 7m })
        {
            var opening = await DraftIdAsync(maker, StockDocumentType.OpeningBalance, room.WarehouseId, null,
                Line(room.ProductId, room.Piece, pieces, unitCost: 2m));
            Assert.True((await DocumentAsync(maker, opening)).GetProperty("requiresApproval").GetBoolean());
            Assert.Equal(HttpStatusCode.OK, (await ActAsync(maker, opening, "submit")).StatusCode);
            Assert.StartsWith("OPN-", (await ActAsync(checker, opening, "approve").PostedAsync()).GetProperty("number").GetString());
        }
        await ReceiveAsync(maker, room, 1m, 2m);

        await (await DraftAsync(maker, StockDocumentType.OpeningBalance, room.WarehouseId, null, Line(room.ProductId, room.Piece, 1m, unitCost: 2m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.OpeningClosed");
        Assert.Equal(13m, await BalanceAsync(maker, room));
    }

    [Fact]
    public async Task A_retry_with_the_same_key_gets_the_first_answer_and_posts_nothing_twice()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        var id = await DraftIdAsync(client, StockDocumentType.Receipt, room.WarehouseId, null, Line(room.ProductId, room.Piece, 6m, unitCost: 1m));
        var rowVersion = (await DocumentAsync(client, id)).GetProperty("rowVersion").GetString();
        var key = Guid.NewGuid();

        var first = await ActAsync(client, id, "post", key, rowVersion: rowVersion).PostedAsync();
        var retry = await ActAsync(client, id, "post", key, rowVersion: rowVersion).PostedAsync();

        Assert.Equal(first.GetProperty("number").GetString(), retry.GetProperty("number").GetString());
        Assert.Equal(first.GetProperty("postedAtUtc").GetDateTime(), retry.GetProperty("postedAtUtc").GetDateTime());
        Assert.Equal(6m, await BalanceAsync(client, room));
        await (await ActAsync(client, id, "post", Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.NotDraft");
        var other = await DraftIdAsync(client, StockDocumentType.Receipt, room.WarehouseId, null, Line(room.ProductId, room.Piece, 1m, unitCost: 1m));
        await (await ActAsync(client, other, "post", key))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Inventory.Idempotency.KeyReused");
        await (await ActAsync(client, other, "post", rowVersion: "AAAAAAAAAAA="))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");

        await using var db = fx.NewDb();
        Assert.Equal(1, await db.Set<StockMovement>().CountAsync(m => m.DocumentId == id));
        Assert.Equal(1, await db.Set<IdempotencyRecord>().CountAsync(r => r.DocumentId == id));
    }

    [Fact]
    public async Task A_reversal_is_approved_by_someone_else_and_undoes_an_issue_at_the_value_it_left_with()
    {
        var maker = Admin();
        var checker = OtherAdmin();
        var room = await CreateStockroomAsync(fx, maker);
        await ReceiveAsync(maker, room, 10m, 10m);
        var issue = await DraftIdAsync(maker, StockDocumentType.Issue, room.WarehouseId, null, Line(room.ProductId, room.Piece, 4m));
        await ActAsync(maker, issue, "post").PostedAsync();
        await ReceiveAsync(maker, room, 4m, 20m); // the average moves to 13 before the reversal

        var reversal = await maker.PostJsonAsync($"{Documents}/{issue}/reverse", new { notes = "صرف بالخطأ" }).CreatedIdAsync();
        var submitted = await DocumentAsync(maker, reversal);
        Assert.Equal(("Reversal", "Submitted", issue), (submitted.GetProperty("documentType").GetString(),
            submitted.GetProperty("status").GetString(), submitted.GetProperty("reversalOfDocumentId").GetGuid()));
        await (await ActAsync(maker, reversal, "approve")).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.SelfApproval");
        Assert.StartsWith("REV-", (await ActAsync(checker, reversal, "approve").PostedAsync()).GetProperty("number").GetString());

        Assert.Equal(reversal, (await DocumentAsync(maker, issue)).GetProperty("reversedByDocumentId").GetGuid());
        Assert.Equal(14m, await BalanceAsync(maker, room));
        var valuation = await ValuationAsync(maker, room.ProductId);
        Assert.Equal((14m, 180m), (valuation.GetProperty("totalQuantity").GetDecimal(), valuation.GetProperty("totalValue").GetDecimal()));
        await (await maker.PostJsonAsync($"{Documents}/{issue}/reverse", new { notes = (string?)null }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Inventory.StockDocument.AlreadyReversed");
        await (await maker.PostJsonAsync($"{Documents}/{reversal}/reverse", new { notes = (string?)null }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.CannotReverseReversal");
    }

    [Fact]
    public async Task An_authorized_loss_may_leave_stock_over_reserved_and_says_so()
    {
        var maker = Admin();
        var room = await CreateStockroomAsync(fx, maker);
        await ReceiveAsync(maker, room, 10m, 1m);
        await using (var db = fx.NewDb()) // reservations arrive with Sales; set one directly
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE inventory.ProductWarehouseStock SET ReservedQuantity = 5 WHERE ProductId = {room.ProductId} AND WarehouseId = {room.WarehouseId}");

        await (await IssueAsync(maker, room, 6m)).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
        var loss = await DraftIdAsync(maker, StockDocumentType.Adjustment, room.WarehouseId, AdjustmentReason.Loss,
            Line(room.ProductId, room.Piece, 8m, direction: StockDirection.Out));
        Assert.Equal(HttpStatusCode.OK, (await ActAsync(maker, loss, "submit")).StatusCode);
        var posted = await ActAsync(OtherAdmin(), loss, "approve").PostedAsync();

        var notice = Assert.Single(posted.GetProperty("overReservations").EnumerateArray());
        Assert.Equal((2m, 5m), (notice.GetProperty("onHandSellable").GetDecimal(), notice.GetProperty("reservedQuantity").GetDecimal()));
        var alerts = (await maker.GetJsonAsync($"/api/inventory/over-reservations?warehouseId={room.WarehouseId}")).GetProperty("items");
        Assert.Equal(loss, Assert.Single(alerts.EnumerateArray()).GetProperty("documentId").GetGuid());
        Assert.Equal(2m, await BalanceAsync(maker, room));
        await (await IssueAsync(maker, room, 1m)).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
    }

    [Fact]
    public async Task Products_the_ledger_cannot_keep_and_quantities_that_do_not_convert_are_refused()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var (batch, batchUnit) = await CreateProductAsync(client, unitId, tracking: TrackingMode.Batch);
        var (service, serviceUnit) = await CreateProductAsync(client, unitId, ProductType.Service);

        await (await DraftAsync(client, StockDocumentType.Receipt, room.WarehouseId, null, Line(batch, batchUnit, 1m, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.TrackingNotSupported");
        await (await DraftAsync(client, StockDocumentType.Receipt, room.WarehouseId, null, Line(service, serviceUnit, 1m, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.ProductNotStockItem");
        await (await DraftAsync(client, StockDocumentType.Receipt, room.WarehouseId, null, Line(room.ProductId, room.Piece, 1.5m, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.QuantityMustBeWhole");
        await (await DraftAsync(client, StockDocumentType.Receipt, room.WarehouseId, null, Line(room.ProductId, batchUnit, 1m, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.ProductUnitNotFound");
        await (await DraftAsync(client, StockDocumentType.Adjustment, room.WarehouseId, AdjustmentReason.Found, Line(room.ProductId, room.Piece, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.DirectionRequired");
        await (await DraftAsync(client, StockDocumentType.Receipt, room.WarehouseId, null, Line(room.ProductId, room.Piece, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.UnitCostRequired");
        var transit = await CreateWarehouseAsync(client, room.BranchId, "Transit");
        await (await DraftAsync(client, StockDocumentType.Receipt, transit, null, Line(room.ProductId, room.Piece, 1m, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.WarehouseIsTransit");
    }

    [Fact]
    public async Task A_draft_changes_only_from_the_version_its_editor_read()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        var id = await DraftIdAsync(client, StockDocumentType.Receipt, room.WarehouseId, null, Line(room.ProductId, room.Piece, 1m, 1m));
        var read = (await DocumentAsync(client, id)).GetProperty("rowVersion").GetString();
        object Update(string? rowVersion, decimal pieces) => new
        {
            adjustmentReason = (AdjustmentReason?)null, externalReference = "EXT-2", notes = "n",
            lines = new[] { Line(room.ProductId, room.Carton, pieces, 30m) }, rowVersion,
        };

        var updated = await (await client.PutJsonAsync($"{Documents}/{id}", Update(read, 2m))).ReadAsync<JsonElement>();
        await (await client.PutJsonAsync($"{Documents}/{id}", Update(read, 3m)))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");

        var document = await DocumentAsync(client, id);
        Assert.Equal(updated.GetProperty("rowVersion").GetString(), document.GetProperty("rowVersion").GetString());
        Assert.Equal((24m, "EXT-2"), (document.GetProperty("lines")[0].GetProperty("baseQuantity").GetDecimal(),
            document.GetProperty("externalReference").GetString()));
        Assert.Equal(HttpStatusCode.OK, (await ActAsync(client, id, "cancel")).StatusCode);
        await (await ActAsync(client, id, "post")).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.NotDraft");
    }

    [Fact]
    public async Task Users_see_and_change_documents_of_their_own_warehouses_with_the_permission_of_the_type()
    {
        var admin = Admin();
        var mine = await CreateStockroomAsync(fx, admin);
        var other = await CreateStockroomAsync(fx, admin);
        var foreign = await DraftIdAsync(admin, StockDocumentType.Receipt, other.WarehouseId, null, Line(other.ProductId, other.Piece, 1m, 1m));
        var storekeeper = _api.CreateClient().As(
            $"{InventoryPermissions.ReceiptsManage},{InventoryPermissions.DocumentsRead},{InventoryPermissions.StockRead}",
            mine.WarehouseId.ToString(), Guid.NewGuid());

        var own = await DraftIdAsync(storekeeper, StockDocumentType.Receipt, mine.WarehouseId, null, Line(mine.ProductId, mine.Piece, 1m, 1m));
        await (await DraftAsync(storekeeper, StockDocumentType.Receipt, other.WarehouseId, null, Line(other.ProductId, other.Piece, 1m, 1m)))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.Warehouse.AccessDenied");
        await (await DraftAsync(storekeeper, StockDocumentType.Issue, mine.WarehouseId, null, Line(mine.ProductId, mine.Piece, 1m)))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.StockDocument.PermissionDenied");
        await (await storekeeper.GetAsync($"{Documents}/{foreign}"))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.StockDocument.AccessDenied");
        await (await storekeeper.GetAsync($"{Documents}/{Guid.NewGuid()}"))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.StockDocument.AccessDenied");
        await (await admin.GetAsync($"{Documents}/{Guid.NewGuid()}"))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Inventory.StockDocument.NotFound");
        await (await storekeeper.GetAsync($"/api/inventory/balances?warehouseId={other.WarehouseId}"))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.Warehouse.AccessDenied");
        await (await storekeeper.GetAsync($"{Documents}/{own}/approve"))
            .AssertProblemAsync(HttpStatusCode.MethodNotAllowed, "Common.MethodNotAllowed");

        var listed = (await storekeeper.GetJsonAsync($"{Documents}?pageSize=100")).GetProperty("items").EnumerateArray()
            .Select(d => d.GetProperty("warehouseId").GetGuid()).Distinct().ToList();
        Assert.Equal([mine.WarehouseId], listed);
        await (await ActAsync(storekeeper, own, "approve", rowVersion: "AAAAAAAAAAA="))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
    }

    [Fact]
    public async Task Approval_policies_change_only_for_new_documents_and_only_by_a_company_wide_user()
    {
        var admin = Admin();
        var room = await CreateStockroomAsync(fx, admin);
        var before = await DraftIdAsync(admin, StockDocumentType.Receipt, room.WarehouseId, null, Line(room.ProductId, room.Piece, 1m, 1m));
        var policies = await admin.GetJsonAsync("/api/inventory/document-policies");
        var receipt = policies.EnumerateArray().Single(p => p.GetProperty("documentType").GetString() == "Receipt");
        Assert.False(receipt.GetProperty("requiresApproval").GetBoolean());
        var scoped = _api.CreateClient().As(InventoryPermissions.SettingsManage, room.WarehouseId.ToString());
        await (await scoped.PutJsonAsync("/api/inventory/document-policies/Receipt",
                new { requiresApproval = true, rowVersion = receipt.GetProperty("rowVersion").GetString() }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.DocumentPolicy.CompanyWideAccessRequired");

        var changed = await (await admin.PutJsonAsync("/api/inventory/document-policies/Receipt",
            new { requiresApproval = true, rowVersion = receipt.GetProperty("rowVersion").GetString() })).ReadAsync<JsonElement>();
        try
        {
            var after = await DraftIdAsync(admin, StockDocumentType.Receipt, room.WarehouseId, null, Line(room.ProductId, room.Piece, 1m, 1m));
            Assert.True((await DocumentAsync(admin, after)).GetProperty("requiresApproval").GetBoolean());
            Assert.False((await DocumentAsync(admin, before)).GetProperty("requiresApproval").GetBoolean());
            await ActAsync(admin, before, "post").PostedAsync();
        }
        finally
        {
            await admin.PutJsonAsync("/api/inventory/document-policies/Receipt",
                new { requiresApproval = false, rowVersion = changed.GetProperty("rowVersion").GetString() });
        }
        await (await admin.PutJsonAsync("/api/inventory/document-policies/Reversal", new { requiresApproval = false, rowVersion = "AAAAAAAAAAA=" }))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
    }
}
