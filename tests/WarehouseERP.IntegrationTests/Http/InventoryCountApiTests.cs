using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Contracts;
using static WarehouseERP.IntegrationTests.Http.InventoryTesting;

namespace WarehouseERP.IntegrationTests.Http;

// Stock counts through the real host on SQL Server: the persisted freeze from start to completion, postings racing a
// start, counted differences posted on approval, and the warehouse scope.
[Collection(DatabaseCollection.Name)]
public sealed class InventoryCountApiTests(DatabaseFixture fx) : IAsyncLifetime
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

    private static string Status(JsonElement count) => count.GetProperty("status").GetString()!;

    [Fact]
    public async Task A_count_freezes_only_its_locations_and_its_approval_posts_the_differences_and_releases_them()
    {
        var admin = Admin();
        var room = await CreateStockroomAsync(fx, admin);
        var shelf = await AddLocationAsync(admin, room.WarehouseId);
        await ReceiveAsync(admin, room, 10m, 4m);
        await ReceiveAsync(admin, room, 5m, 4m, locationId: shelf);
        var (found, _) = await CreateProductAsync(admin, await CatalogTesting.CreateUnitAsync(admin));

        var id = await DraftCountAsync(admin, room.WarehouseId, room.DefaultLocationId).CreatedIdAsync();
        var draft = await CountAsync(admin, id);
        Assert.Equal(("Draft", false, 0), (Status(draft), draft.GetProperty("isWholeWarehouse").GetBoolean(),
            draft.GetProperty("lines").GetArrayLength()));
        Assert.Matches("^CT-[0-9]{6,}$", draft.GetProperty("reference").GetString());
        Assert.Equal("DEFAULT", draft.GetProperty("locations")[0].GetProperty("locationCode").GetString());
        await IssueAsync(admin, room, 1m).PostedAsync(); // nothing is frozen before the start

        await CountActAsync(admin, id, "start").EnsureOkAsync();

        var counting = await CountAsync(admin, id);
        var expected = Assert.Single(counting.GetProperty("lines").EnumerateArray());
        Assert.Equal(("Counting", room.ProductId, room.DefaultLocationId, 9m, JsonValueKind.Null),
            (Status(counting), expected.GetProperty("productId").GetGuid(), expected.GetProperty("locationId").GetGuid(),
                expected.GetProperty("expectedQuantity").GetDecimal(), expected.GetProperty("countedQuantity").ValueKind));
        var frozen = await (await IssueAsync(admin, room, 1m))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockCount.ScopeFrozen");
        Assert.Contains(counting.GetProperty("reference").GetString()!, frozen.GetProperty("detail").GetString());
        await IssueAsync(admin, room, 1m, shelf).PostedAsync(); // the shelf is not counted

        await (await EnterCountsAsync(admin, id, Counted(room.ProductId, shelf, 4m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockCount.OutOfScope");
        await (await CountActAsync(admin, id, "submit")).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockCount.NotFullyCounted");
        await EnterCountsAsync(admin, id, Counted(room.ProductId, room.DefaultLocationId, 7m),
            Counted(found, room.DefaultLocationId, 2m, unitCost: 3m)).EnsureOkAsync();
        await CountActAsync(admin, id, "submit").EnsureOkAsync();

        var key = Guid.NewGuid(); // its creator approves
        var submitted = (await CountAsync(admin, id)).GetProperty("rowVersion").GetString();
        var approved = await CountActAsync(admin, id, "approve", key, rowVersion: submitted).PostedAsync();
        var number = approved.GetProperty("adjustmentNumber").GetString()!;
        Assert.StartsWith("CNT-B-", number);
        var replay = await CountActAsync(admin, id, "approve", key, rowVersion: submitted).PostedAsync();
        Assert.Equal(number, replay.GetProperty("adjustmentNumber").GetString());

        var completed = await CountAsync(admin, id);
        Assert.Equal(("Completed", number), (Status(completed), completed.GetProperty("adjustmentNumber").GetString()));
        Assert.Equal([-2m, 2m], completed.GetProperty("lines").EnumerateArray().Select(l => l.GetProperty("variance").GetDecimal()).Order());
        var adjustment = await DocumentAsync(admin, completed.GetProperty("adjustmentDocumentId").GetGuid());
        Assert.Equal(("CountAdjustment", "Posted", 2, id), (adjustment.GetProperty("documentType").GetString(),
            adjustment.GetProperty("status").GetString(), adjustment.GetProperty("lines").GetArrayLength(),
            adjustment.GetProperty("countId").GetGuid()));
        Assert.All(adjustment.GetProperty("lines").EnumerateArray(), l => Assert.Equal(JsonValueKind.Null, l.GetProperty("fromLocationId").ValueKind));
        Assert.Equal(7m, await BalanceAsync(admin, room, room.DefaultLocationId));
        Assert.Equal(2m, await QuantityAsync(admin, room.WarehouseId, found, room.DefaultLocationId));
        await AssertValuedAsync(admin, room.ProductId, 11m, 44m); // 15 in at 4, 2 issued and 2 short at the average
        await AssertValuedAsync(admin, found, 2m, 6m);

        await IssueAsync(admin, room, 1m).PostedAsync(); // released
    }

    [Fact]
    public async Task A_whole_warehouse_count_holds_every_posting_and_any_overlapping_count_until_it_is_cancelled()
    {
        var admin = Admin();
        var room = await CreateStockroomAsync(fx, admin);
        await ReceiveAsync(admin, room, 3m, 2m);
        var whole = await DraftCountAsync(admin, room.WarehouseId).CreatedIdAsync();
        var part = await DraftCountAsync(admin, room.WarehouseId, room.DefaultLocationId).CreatedIdAsync();

        await CountActAsync(admin, whole, "start").EnsureOkAsync();

        Assert.True((await CountAsync(admin, whole)).GetProperty("isWholeWarehouse").GetBoolean());
        await (await CountActAsync(admin, part, "start")).AssertProblemAsync(HttpStatusCode.Conflict, "Inventory.StockCount.ScopeOverlaps");
        var receipt = await DraftIdAsync(admin, StockDocumentType.Receipt, room.WarehouseId, null, Line(room.ProductId, room.Piece, 1m, 2m));
        await (await ActAsync(admin, receipt, "post")).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockCount.ScopeFrozen");
        var elsewhere = await CreateStockroomAsync(fx, admin);
        await ReceiveAsync(admin, elsewhere, 1m, 1m); // other warehouses are not frozen
        var empty = await CreateWarehouseAsync(admin, room.BranchId);
        var emptyCount = await DraftCountAsync(admin, empty).CreatedIdAsync();
        await CountActAsync(admin, emptyCount, "start").EnsureOkAsync();
        await (await admin.PostEmptyAsync($"{Warehouses}/{empty}/deactivate"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Warehouse.HasOpenInventoryDocuments");
        await CountActAsync(admin, emptyCount, "cancel").EnsureOkAsync();

        await CountActAsync(admin, whole, "cancel").EnsureOkAsync();

        Assert.Equal("Cancelled", Status(await CountAsync(admin, whole)));
        await ActAsync(admin, receipt, "post").PostedAsync();
        await CountActAsync(admin, part, "start").EnsureOkAsync();
        Assert.Equal(4m, (await CountAsync(admin, part)).GetProperty("lines")[0].GetProperty("expectedQuantity").GetDecimal());
        await CountActAsync(admin, part, "cancel").EnsureOkAsync();
    }

    [Fact]
    public async Task A_posting_racing_a_count_start_lands_before_the_snapshot_or_is_refused_never_after()
    {
        var admin = Admin();
        var room = await CreateStockroomAsync(fx, admin);
        await ReceiveAsync(admin, room, 50m, 1m);

        for (var round = 0; round < 6; round++)
        {
            var id = await DraftCountAsync(admin, room.WarehouseId).CreatedIdAsync();
            var version = (await CountAsync(admin, id)).GetProperty("rowVersion").GetString();

            var start = CountActAsync(_api.CreateClient().As(AllPermissions), id, "start", rowVersion: version);
            var issue = IssueAsync(_api.CreateClient().As(AllPermissions), room, 1m);
            await Task.WhenAll(start, issue);

            var count = await CountAsync(admin, id);
            var balance = await BalanceAsync(admin, room);
            if (Status(count) == "Counting")
            {
                var snapshot = count.GetProperty("lines").EnumerateArray().Sum(l => l.GetProperty("expectedQuantity").GetDecimal());
                Assert.Equal(balance, snapshot);
                if ((await issue).StatusCode != HttpStatusCode.OK)
                    await (await issue).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockCount.ScopeFrozen");
            }
            else
            {
                Assert.Equal("Draft", Status(count));
                await (await start).AssertProblemAsync(HttpStatusCode.Conflict, "Inventory.Stock.Busy");
            }
            await CountActAsync(admin, id, "cancel").EnsureOkAsync();
        }
    }

    [Fact]
    public async Task Approval_refuses_a_count_whose_frozen_stock_changed_behind_its_back()
    {
        var admin = Admin();
        var other = OtherAdmin();
        var room = await CreateStockroomAsync(fx, admin);
        await ReceiveAsync(admin, room, 4m, 1m);
        var id = await DraftCountAsync(admin, room.WarehouseId, room.DefaultLocationId).CreatedIdAsync();
        await CountActAsync(admin, id, "start").EnsureOkAsync();
        await EnterCountsAsync(admin, id, Counted(room.ProductId, room.DefaultLocationId, 4m)).EnsureOkAsync();
        await CountActAsync(admin, id, "submit").EnsureOkAsync();

        await ShiftBalanceAsync(room, +1m); // nothing in the API can do this while the location is frozen
        try
        {
            await (await CountActAsync(other, id, "approve"))
                .AssertProblemAsync(HttpStatusCode.Conflict, "Inventory.StockCount.StockChangedDuringCount");
            Assert.Equal("Submitted", Status(await CountAsync(admin, id)));
        }
        finally
        {
            await ShiftBalanceAsync(room, -1m);
        }

        await CountActAsync(other, id, "approve").PostedAsync();
        Assert.Equal(JsonValueKind.Null, (await CountAsync(admin, id)).GetProperty("adjustmentNumber").ValueKind); // no differences
    }

    [Fact]
    public async Task Counts_belong_to_their_warehouse_scope()
    {
        var admin = Admin();
        var room = await CreateStockroomAsync(fx, admin);
        var other = await CreateStockroomAsync(fx, admin);
        var counter = _api.CreateClient().As(
            $"{InventoryPermissions.CountsManage},{InventoryPermissions.DocumentsRead}", room.WarehouseId.ToString(), Guid.NewGuid());

        var own = await DraftCountAsync(counter, room.WarehouseId).CreatedIdAsync();
        var foreign = await DraftCountAsync(admin, other.WarehouseId).CreatedIdAsync();

        await (await DraftCountAsync(counter, other.WarehouseId)).AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.Warehouse.AccessDenied");
        await (await DraftCountAsync(admin, room.WarehouseId, other.DefaultLocationId))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockCount.LocationNotFound");
        await (await counter.GetAsync($"{Counts}/{foreign}")).AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.StockCount.AccessDenied");
        await (await counter.GetAsync($"{Counts}/{Guid.NewGuid()}")).AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.StockCount.AccessDenied");
        await (await admin.GetAsync($"{Counts}/{Guid.NewGuid()}")).AssertProblemAsync(HttpStatusCode.NotFound, "Inventory.StockCount.NotFound");
        await (await CountActAsync(counter, foreign, "start", rowVersion: "AAAAAAAAAAA="))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.StockCount.AccessDenied");
        var listed = (await counter.GetJsonAsync($"{Counts}?pageSize=100")).GetProperty("items").EnumerateArray()
            .Select(c => c.GetProperty("warehouseId").GetGuid()).Distinct().ToList();
        Assert.Equal([room.WarehouseId], listed);
        await CountActAsync(counter, own, "start").EnsureOkAsync();
        await (await CountActAsync(counter, own, "approve")).AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
        await CountActAsync(counter, own, "cancel").EnsureOkAsync();
        Assert.Contains(own, (await admin.GetJsonAsync($"{Counts}?warehouseId={room.WarehouseId}&status={StockCountStatus.Cancelled}"))
            .GetProperty("items").EnumerateArray().Select(c => c.GetProperty("id").GetGuid()));
    }

    private async Task ShiftBalanceAsync(Stockroom room, decimal by)
    {
        await using var db = fx.NewDb();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [inventory].[StockBalances] SET [Quantity] = [Quantity] + {by} WHERE [WarehouseId] = {room.WarehouseId} AND [ProductId] = {room.ProductId}");
    }
}
