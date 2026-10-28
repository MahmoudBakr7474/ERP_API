using System.Net;
using System.Text.Json;
using WarehouseERP.Inventory.Contracts;
using static WarehouseERP.IntegrationTests.Http.InventoryTesting;

namespace WarehouseERP.IntegrationTests.Http;

// Transfers through the real host on SQL Server: dispatch into the transfer's own transit location, partial receipts,
// the loss that closes what never arrived, approval, retries, races and the warehouse scope of each side.
[Collection(DatabaseCollection.Name)]
public sealed class InventoryTransferApiTests(DatabaseFixture fx) : IAsyncLifetime
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

    private static string Status(JsonElement transfer) => transfer.GetProperty("status").GetString()!;

    [Fact]
    public async Task A_transfer_moves_stock_through_its_own_transit_location_and_never_changes_the_company_valuation()
    {
        var admin = Admin();
        var route = await CreateRouteAsync(fx, admin);
        var room = route.Source;
        await ReceiveAsync(admin, room, 20m, 5m);

        var id = await DraftTransferAsync(admin, route, TransferLine(room.ProductId, room.Carton, 1m)).CreatedIdAsync();
        var draft = await TransferAsync(admin, id);
        Assert.Equal(("Draft", false, route.TransitId, JsonValueKind.Null, JsonValueKind.Null),
            (Status(draft), draft.GetProperty("requiresApproval").GetBoolean(), draft.GetProperty("transitWarehouseId").GetGuid(),
                draft.GetProperty("number").ValueKind, draft.GetProperty("transitLocationId").ValueKind));
        Assert.Matches("^TR-[0-9]{6,}$", draft.GetProperty("reference").GetString());
        var line = draft.GetProperty("lines")[0];
        Assert.Equal((1m, 12m, room.DefaultLocationId, "DEFAULT"),
            (line.GetProperty("quantity").GetDecimal(), line.GetProperty("baseQuantity").GetDecimal(),
                line.GetProperty("sourceLocationId").GetGuid(), line.GetProperty("sourceLocationCode").GetString()));

        var key = Guid.NewGuid();
        var draftVersion = draft.GetProperty("rowVersion").GetString();
        var dispatched = await TransferActAsync(admin, id, "dispatch", key, rowVersion: draftVersion).PostedAsync();
        var number = dispatched.GetProperty("number").GetString()!;
        Assert.StartsWith("TRF-B-", number);
        Assert.EndsWith("-2026-000001", number);
        Assert.Equal("InTransit", Status(dispatched));
        var replay = await TransferActAsync(admin, id, "dispatch", key, rowVersion: draftVersion).PostedAsync();
        Assert.Equal((number, dispatched.GetProperty("documentId").GetGuid()),
            (replay.GetProperty("number").GetString(), replay.GetProperty("documentId").GetGuid()));

        var transfer = await TransferAsync(admin, id);
        var transitLocation = transfer.GetProperty("transitLocationId").GetGuid();
        Assert.Equal((number, 12m), (transfer.GetProperty("number").GetString(), transfer.GetProperty("outstandingQuantity").GetDecimal()));
        Assert.StartsWith("TR-", transfer.GetProperty("transitLocationCode").GetString());
        Assert.Equal(8m, await QuantityAsync(admin, room.WarehouseId, room.ProductId));
        Assert.Equal(12m, await QuantityAsync(admin, route.TransitId, room.ProductId, transitLocation));
        Assert.Equal(0m, await QuantityAsync(admin, route.DestinationId, room.ProductId));
        await AssertValuedAsync(admin, room.ProductId, 20m, 100m);

        var receiver = OtherAdmin();
        var lineId = transfer.GetProperty("lines")[0].GetProperty("id").GetGuid();
        var partly = await ReceiveTransferAsync(receiver, id, null, null, (lineId, 5m)).PostedAsync();
        Assert.Equal("PartiallyReceived", Status(partly));
        Assert.StartsWith("TRR-B-", partly.GetProperty("number").GetString());
        Assert.Equal(7m, await QuantityAsync(admin, route.TransitId, room.ProductId, transitLocation));
        Assert.Equal(5m, await QuantityAsync(admin, route.DestinationId, room.ProductId, route.DestinationLocationId));

        var restKey = Guid.NewGuid();
        var restVersion = (await TransferAsync(receiver, id)).GetProperty("rowVersion").GetString();
        var rest = await ReceiveTransferAsync(receiver, id, restKey, restVersion, (lineId, 7m)).PostedAsync();
        Assert.Equal("Received", Status(rest));
        var again = await ReceiveTransferAsync(receiver, id, restKey, restVersion, (lineId, 7m)).PostedAsync();
        Assert.Equal(rest.GetProperty("documentId").GetGuid(), again.GetProperty("documentId").GetGuid());
        await (await ReceiveTransferAsync(receiver, id, restKey, restVersion, (lineId, 6m)))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Inventory.Idempotency.KeyReused");

        var received = await TransferAsync(admin, id);
        var receivedLine = received.GetProperty("lines")[0];
        Assert.Equal((0m, 12m, 0m), (received.GetProperty("outstandingQuantity").GetDecimal(),
            receivedLine.GetProperty("receivedQuantity").GetDecimal(), receivedLine.GetProperty("lostQuantity").GetDecimal()));
        Assert.Equal(["TransferDispatch", "TransferReceipt", "TransferReceipt"],
            received.GetProperty("documents").EnumerateArray().Select(d => d.GetProperty("documentType").GetString()));
        Assert.Equal(["Created", "Dispatched", "Received", "Received"],
            received.GetProperty("events").EnumerateArray().Select(e => e.GetProperty("action").GetString()));
        Assert.Equal(0m, await QuantityAsync(admin, route.TransitId, room.ProductId));
        Assert.Equal(12m, await QuantityAsync(admin, route.DestinationId, room.ProductId));
        await AssertValuedAsync(admin, room.ProductId, 20m, 100m);
        Assert.Equal(1, (await admin.GetJsonAsync($"/api/inventory/valuation/{room.ProductId}/entries")).GetProperty("totalCount").GetInt32());
        var retired = (await admin.GetJsonAsync($"{Warehouses}/{route.TransitId}/locations?isActive=false")).GetProperty("items");
        Assert.Contains(retired.EnumerateArray(), l => l.GetProperty("id").GetGuid() == transitLocation);
    }

    [Fact]
    public async Task Dispatch_follows_the_approval_policy_of_its_creation_and_nobody_approves_a_transfer_they_created()
    {
        var admin = Admin();
        var other = OtherAdmin();
        var route = await CreateRouteAsync(fx, admin);
        await ReceiveAsync(admin, route.Source, 10m, 2m);
        var policy = (await admin.GetJsonAsync("/api/inventory/document-policies")).EnumerateArray()
            .Single(p => p.GetProperty("documentType").GetString() == "TransferDispatch");
        Assert.False(policy.GetProperty("requiresApproval").GetBoolean());
        var changed = await (await admin.PutJsonAsync("/api/inventory/document-policies/TransferDispatch",
            new { requiresApproval = true, rowVersion = policy.GetProperty("rowVersion").GetString() })).ReadAsync<JsonElement>();
        Guid id;
        try
        {
            id = await DraftTransferAsync(admin, route, TransferLine(route.Source.ProductId, route.Source.Piece, 4m)).CreatedIdAsync();
        }
        finally
        {
            Assert.Equal(HttpStatusCode.OK, (await admin.PutJsonAsync("/api/inventory/document-policies/TransferDispatch",
                new { requiresApproval = false, rowVersion = changed.GetProperty("rowVersion").GetString() })).StatusCode);
        }

        Assert.True((await TransferAsync(admin, id)).GetProperty("requiresApproval").GetBoolean());
        await (await TransferActAsync(admin, id, "dispatch"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockTransfer.ApprovalRequired");
        await (await TransferActAsync(other, id, "approve"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockTransfer.NotSubmitted");
        await TransferActAsync(admin, id, "submit").EnsureOkAsync();
        await (await TransferActAsync(admin, id, "approve"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockTransfer.SelfApproval");
        var submitted = (await TransferAsync(other, id)).GetProperty("rowVersion").GetString();
        await TransferActAsync(other, id, "reject", body: new { reason = "check the quantity", rowVersion = submitted }).EnsureOkAsync();
        Assert.Equal("Draft", Status(await TransferAsync(admin, id)));
        await TransferActAsync(admin, id, "submit").EnsureOkAsync();

        var approved = await TransferActAsync(other, id, "approve").PostedAsync();

        Assert.Equal("InTransit", Status(approved));
        var transfer = await TransferAsync(admin, id);
        Assert.Equal(["Created", "Submitted", "Rejected", "Submitted", "Approved", "Dispatched"],
            transfer.GetProperty("events").EnumerateArray().Select(e => e.GetProperty("action").GetString()));
        Assert.Equal("check the quantity", transfer.GetProperty("events")[2].GetProperty("note").GetString());
        Assert.Equal(6m, await QuantityAsync(admin, route.Source.WarehouseId, route.Source.ProductId));
        Assert.Equal(4m, await QuantityAsync(admin, route.TransitId, route.Source.ProductId));
    }

    [Fact]
    public async Task What_never_arrives_is_written_off_by_an_approved_loss_and_nothing_is_received_meanwhile()
    {
        var admin = Admin();
        var other = OtherAdmin();
        var route = await CreateRouteAsync(fx, admin);
        var room = route.Source;
        await ReceiveAsync(admin, room, 10m, 6m);
        var (id, lineId) = await DispatchPiecesAsync(admin, route, 10m);
        await ReceiveTransferAsync(other, id, null, null, (lineId, 6m)).PostedAsync();

        var first = await CloseTransferAsync(admin, id).CreatedIdAsync();
        Assert.Equal("Closing", Status(await TransferAsync(admin, id)));
        await (await ReceiveTransferAsync(other, id, null, null, (lineId, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockTransfer.NotInTransit");
        var loss = await DocumentAsync(admin, first);
        Assert.Equal(("Adjustment", "Submitted", "Loss", route.TransitId, 4m),
            (loss.GetProperty("documentType").GetString(), loss.GetProperty("status").GetString(),
                loss.GetProperty("adjustmentReason").GetString(), loss.GetProperty("warehouseId").GetGuid(),
                loss.GetProperty("lines")[0].GetProperty("baseQuantity").GetDecimal()));
        await ActAsync(admin, first, "cancel").EnsureOkAsync();
        Assert.Equal("PartiallyReceived", Status(await TransferAsync(admin, id)));

        var second = await CloseTransferAsync(admin, id).CreatedIdAsync();
        await (await ActAsync(admin, second, "approve"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.SelfApproval");
        await ActAsync(other, second, "approve").PostedAsync();

        var closed = await TransferAsync(admin, id);
        var line = closed.GetProperty("lines")[0];
        Assert.Equal(("Closed", 0m, 6m, 4m), (Status(closed), closed.GetProperty("outstandingQuantity").GetDecimal(),
            line.GetProperty("receivedQuantity").GetDecimal(), line.GetProperty("lostQuantity").GetDecimal()));
        Assert.Equal(0m, await QuantityAsync(admin, route.TransitId, room.ProductId));
        Assert.Equal(6m, await QuantityAsync(admin, route.DestinationId, room.ProductId));
        await AssertValuedAsync(admin, room.ProductId, 6m, 36m);
        await (await CloseTransferAsync(admin, id))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockTransfer.NotInTransit");
        var lossVersion = (await DocumentAsync(other, second)).GetProperty("rowVersion").GetString();
        await (await ActAsync(other, second, "reverse", body: new { notes = (string?)null }, rowVersion: lossVersion))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.NotReversible");
    }

    [Fact]
    public async Task A_dispatch_never_takes_more_than_the_source_location_has_and_a_refused_one_leaves_nothing_behind()
    {
        var admin = Admin();
        var route = await CreateRouteAsync(fx, admin);
        await ReceiveAsync(admin, route.Source, 5m, 1m);
        var id = await DraftTransferAsync(admin, route, TransferLine(route.Source.ProductId, route.Source.Piece, 6m)).CreatedIdAsync();

        var refused = await TransferActAsync(admin, id, "dispatch");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.StartsWith("Inventory.Stock.Insufficient", (await refused.ReadAsync<JsonElement>()).GetProperty("errorCode").GetString());
        var transfer = await TransferAsync(admin, id);
        Assert.Equal(("Draft", JsonValueKind.Null, 0),
            (Status(transfer), transfer.GetProperty("transitLocationId").ValueKind, transfer.GetProperty("documents").GetArrayLength()));
        Assert.Single((await admin.GetJsonAsync($"{Warehouses}/{route.TransitId}/locations")).GetProperty("items").EnumerateArray());
        Assert.Equal(5m, await QuantityAsync(admin, route.Source.WarehouseId, route.Source.ProductId));
        await AssertValuedAsync(admin, route.Source.ProductId, 5m, 5m);
        await (await DraftTransferAsync(admin, route with { DestinationId = route.Source.WarehouseId },
                TransferLine(route.Source.ProductId, route.Source.Piece, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockTransfer.SameWarehouse");
        await (await DraftTransferAsync(admin, route with { DestinationId = route.TransitId },
                TransferLine(route.Source.ProductId, route.Source.Piece, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockTransfer.DestinationNotUsable");
    }

    [Fact]
    public async Task The_source_side_prepares_and_dispatches_the_destination_side_receives_and_nobody_else_sees_it()
    {
        var admin = Admin();
        var route = await CreateRouteAsync(fx, admin);
        var room = route.Source;
        await ReceiveAsync(admin, room, 10m, 1m);
        var sender = _api.CreateClient().As(
            $"{InventoryPermissions.TransfersManage},{InventoryPermissions.DocumentsRead}", room.WarehouseId.ToString(), Guid.NewGuid());
        var receiver = _api.CreateClient().As(
            $"{InventoryPermissions.TransfersReceive},{InventoryPermissions.DocumentsRead}", route.DestinationId.ToString(), Guid.NewGuid());
        var elsewhere = await CreateStockroomAsync(fx, admin);
        var outsider = _api.CreateClient().As(AllPermissions, elsewhere.WarehouseId.ToString(), Guid.NewGuid());

        var id = await DraftTransferAsync(sender, route, TransferLine(room.ProductId, room.Piece, 4m)).CreatedIdAsync();

        await (await DraftTransferAsync(outsider, route, TransferLine(room.ProductId, room.Piece, 1m)))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.Warehouse.AccessDenied");
        await (await outsider.GetAsync($"{Transfers}/{id}")).AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.StockTransfer.AccessDenied");
        await (await outsider.GetAsync($"{Transfers}/{Guid.NewGuid()}"))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.StockTransfer.AccessDenied");
        await (await admin.GetAsync($"{Transfers}/{Guid.NewGuid()}")).AssertProblemAsync(HttpStatusCode.NotFound, "Inventory.StockTransfer.NotFound");
        Assert.DoesNotContain(id, await ListedAsync(outsider));
        Assert.Contains(id, await ListedAsync(receiver)); // the destination sees what is coming
        await (await TransferActAsync(receiver, id, "dispatch")).AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
        await (await TransferActAsync(outsider, id, "dispatch", rowVersion: "AAAAAAAAAAA="))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.StockTransfer.AccessDenied");

        await TransferActAsync(sender, id, "dispatch").PostedAsync();
        var lineId = (await TransferAsync(receiver, id)).GetProperty("lines")[0].GetProperty("id").GetGuid();

        await (await ReceiveTransferAsync(sender, id, null, null, (lineId, 4m))).AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
        await (await ReceiveTransferAsync(outsider, id, null, "AAAAAAAAAAA=", (lineId, 4m)))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.StockTransfer.AccessDenied");
        await ReceiveTransferAsync(receiver, id, null, null, (lineId, 4m)).PostedAsync();
        Assert.Equal(4m, await QuantityAsync(admin, route.DestinationId, room.ProductId));
    }

    [Fact]
    public async Task Receipts_racing_for_what_is_left_in_transit_post_it_once()
    {
        var admin = Admin();
        var route = await CreateRouteAsync(fx, admin);
        await ReceiveAsync(admin, route.Source, 8m, 1m);
        var (id, lineId) = await DispatchPiecesAsync(admin, route, 8m);
        var version = (await TransferAsync(admin, id)).GetProperty("rowVersion").GetString();

        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            ReceiveTransferAsync(_api.CreateClient().As(AllPermissions), id, Guid.NewGuid(), version, (lineId, 8m))));

        Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK);
        Assert.All(results.Where(r => r.StatusCode != HttpStatusCode.OK),
            r => Assert.True(r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity, r.StatusCode.ToString()));
        Assert.Equal(8m, await QuantityAsync(admin, route.DestinationId, route.Source.ProductId));
        Assert.Equal(0m, await QuantityAsync(admin, route.TransitId, route.Source.ProductId));
        Assert.Equal(2, (await TransferAsync(admin, id)).GetProperty("documents").GetArrayLength());
    }

    [Fact]
    public async Task A_running_count_holds_a_transfer_at_either_end_until_it_is_cancelled()
    {
        var admin = Admin();
        var route = await CreateRouteAsync(fx, admin);
        await ReceiveAsync(admin, route.Source, 6m, 1m);
        var (id, lineId) = await DispatchPiecesAsync(admin, route, 2m);
        var next = await DraftTransferAsync(admin, route, TransferLine(route.Source.ProductId, route.Source.Piece, 2m)).CreatedIdAsync();
        var sourceCount = await DraftCountAsync(admin, route.Source.WarehouseId, route.Source.DefaultLocationId).CreatedIdAsync();
        var destinationCount = await DraftCountAsync(admin, route.DestinationId).CreatedIdAsync();
        await CountActAsync(admin, sourceCount, "start").EnsureOkAsync();
        await CountActAsync(admin, destinationCount, "start").EnsureOkAsync();

        await (await TransferActAsync(admin, next, "dispatch"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockCount.ScopeFrozen");
        await (await ReceiveTransferAsync(admin, id, null, null, (lineId, 2m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockCount.ScopeFrozen");
        Assert.Equal(2m, await QuantityAsync(admin, route.TransitId, route.Source.ProductId)); // transit is never counted

        await CountActAsync(admin, sourceCount, "cancel").EnsureOkAsync();
        await CountActAsync(admin, destinationCount, "cancel").EnsureOkAsync();

        await TransferActAsync(admin, next, "dispatch").PostedAsync();
        await ReceiveTransferAsync(admin, id, null, null, (lineId, 2m)).PostedAsync();
        Assert.Equal(2m, await QuantityAsync(admin, route.Source.WarehouseId, route.Source.ProductId));
        Assert.Equal(2m, await QuantityAsync(admin, route.DestinationId, route.Source.ProductId));
    }

    private static async Task<List<Guid>> ListedAsync(HttpClient client) =>
        (await client.GetJsonAsync($"{Transfers}?pageSize=100")).GetProperty("items").EnumerateArray()
            .Select(t => t.GetProperty("id").GetGuid()).ToList();
}
