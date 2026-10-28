using System.Net;
using System.Text.Json;
using WarehouseERP.Inventory.Contracts;
using static WarehouseERP.IntegrationTests.Http.InventoryTesting;

namespace WarehouseERP.IntegrationTests.Http;

// The global approval rule (CLAUDE.md, Document approvals) on SQL Server: a warehouse-scoped user without company-wide
// access approves what they created when they hold inventory.documents.approve: their stock count (which posts its
// count adjustment), adjustment and the reversal of it. The history names them as creator and approver. Without the permission,
// or outside the warehouse, approving is still refused.
[Collection(DatabaseCollection.Name)]
public sealed class InventorySelfApprovalTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(AllPermissions);

    private HttpClient ScopedUser(Guid userId, Guid warehouseId, params string[] permissions) =>
        _api.CreateClient().As(string.Join(',', permissions), warehouseId.ToString(), userId);

    private static Guid EventUser(JsonElement record, string action) =>
        record.GetProperty("events").EnumerateArray()
            .Single(e => e.GetProperty("action").GetString() == action).GetProperty("userId").GetGuid();

    [Fact]
    public async Task A_scoped_counter_with_the_approval_permission_approves_their_own_count_and_its_adjustment_posts()
    {
        var admin = Admin();
        var room = await CreateStockroomAsync(fx, admin);
        await ReceiveAsync(admin, room, 10m, 5m);
        var keeperId = Guid.NewGuid();
        var keeper = ScopedUser(keeperId, room.WarehouseId,
            InventoryPermissions.CountsManage, InventoryPermissions.DocumentsApprove, InventoryPermissions.DocumentsRead,
            InventoryPermissions.StockRead);

        var id = await DraftCountAsync(keeper, room.WarehouseId).CreatedIdAsync();
        await CountActAsync(keeper, id, "start").EnsureOkAsync();
        await EnterCountsAsync(keeper, id, Counted(room.ProductId, room.DefaultLocationId, 8m)).EnsureOkAsync();
        await CountActAsync(keeper, id, "submit").EnsureOkAsync();
        var approved = await CountActAsync(keeper, id, "approve").PostedAsync();

        var count = await CountAsync(keeper, id);
        Assert.Equal(("Completed", keeperId), (count.GetProperty("status").GetString(), count.GetProperty("createdByUserId").GetGuid()));
        Assert.Equal(keeperId, EventUser(count, "Submitted"));
        Assert.Equal(keeperId, EventUser(count, "Approved"));
        var adjustment = await DocumentAsync(keeper, approved.GetProperty("adjustmentDocumentId").GetGuid());
        Assert.Equal(("Posted", "CountAdjustment"), (adjustment.GetProperty("status").GetString(), adjustment.GetProperty("documentType").GetString()));
        Assert.Equal(keeperId, EventUser(adjustment, "Approved"));
        Assert.Equal(8m, await BalanceAsync(keeper, room));
    }

    [Fact]
    public async Task A_scoped_user_approves_their_own_adjustment_and_their_own_reversal_of_it()
    {
        var admin = Admin();
        var room = await CreateStockroomAsync(fx, admin);
        var userId = Guid.NewGuid();
        var user = ScopedUser(userId, room.WarehouseId,
            InventoryPermissions.ReceiptsManage, InventoryPermissions.AdjustmentsManage, InventoryPermissions.DocumentsApprove,
            InventoryPermissions.DocumentsRead, InventoryPermissions.StockRead);
        await ReceiveAsync(user, room, 10m, 5m);

        var adjustment = await DraftIdAsync(user, StockDocumentType.Adjustment, room.WarehouseId, AdjustmentReason.Damage,
            Line(room.ProductId, room.Piece, 1m, direction: StockDirection.Out));
        await ActAsync(user, adjustment, "submit").EnsureOkAsync();
        await ActAsync(user, adjustment, "approve").PostedAsync();
        var posted = await DocumentAsync(user, adjustment);
        Assert.Equal(("Posted", userId, userId), (posted.GetProperty("status").GetString(),
            posted.GetProperty("createdByUserId").GetGuid(), EventUser(posted, "Approved")));

        var reversal = await user.PostJsonAsync($"{Documents}/{adjustment}/reverse", new { notes = "not damaged after all" }).CreatedIdAsync();
        await ActAsync(user, reversal, "approve").PostedAsync();
        var reversed = await DocumentAsync(user, reversal);
        Assert.Equal(("Posted", userId), (reversed.GetProperty("status").GetString(), EventUser(reversed, "Approved")));
        Assert.Equal(10m, await BalanceAsync(user, room));
    }

    [Fact]
    public async Task Approving_still_needs_the_permission_and_the_warehouse_in_scope()
    {
        var admin = Admin();
        var room = await CreateStockroomAsync(fx, admin);
        await ReceiveAsync(admin, room, 5m, 5m);
        var counterId = Guid.NewGuid();
        var counter = ScopedUser(counterId, room.WarehouseId, InventoryPermissions.CountsManage, InventoryPermissions.DocumentsRead);
        var id = await DraftCountAsync(counter, room.WarehouseId).CreatedIdAsync();
        await CountActAsync(counter, id, "start").EnsureOkAsync();
        await EnterCountsAsync(counter, id, Counted(room.ProductId, room.DefaultLocationId, 5m)).EnsureOkAsync();
        await CountActAsync(counter, id, "submit").EnsureOkAsync();

        var rowVersion = (await CountAsync(counter, id)).GetProperty("rowVersion").GetString();
        await (await CountActAsync(counter, id, "approve", rowVersion: rowVersion))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
        var outsider = ScopedUser(Guid.NewGuid(), Guid.NewGuid(), InventoryPermissions.DocumentsApprove, InventoryPermissions.DocumentsRead);
        await (await CountActAsync(outsider, id, "approve", rowVersion: rowVersion))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.StockCount.AccessDenied");
        await (await CountActAsync(counter, id, "start", rowVersion: rowVersion))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockCount.NotDraft");

        await CountActAsync(admin, id, "approve", rowVersion: rowVersion).PostedAsync(); // another approver may too
        var count = await CountAsync(admin, id);
        Assert.Equal(("Completed", HttpTesting.UserId), (count.GetProperty("status").GetString(), EventUser(count, "Approved")));
    }
}
