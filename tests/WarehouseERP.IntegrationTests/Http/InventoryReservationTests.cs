using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;
using static WarehouseERP.IntegrationTests.Http.InventoryTesting;

namespace WarehouseERP.IntegrationTests.Http;

// Inventory's reservation and sales posting contracts (IStockReservations, ISalesStockPosting) on SQL Server, called the
// way Sales calls them: inside a transaction that saves on success and rolls back on a refusal. Concurrent reservations
// never hold more than is on hand, an ordinary issue can't take reserved stock, releases give back only what remains,
// deliveries consume their own reservation and need the stock in the picked location, an over-reserving loss leaves
// every reservation alone, and returns come back at their delivery's cost.
[Collection(DatabaseCollection.Name)]
public sealed class InventoryReservationTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(AllPermissions);

    [Fact]
    public async Task Concurrent_reservations_of_the_last_units_hold_only_what_is_on_hand()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        await ReceiveAsync(client, room, 5m, 1m);

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => ReserveAsync(room, Guid.NewGuid(), (Guid.NewGuid(), 2m))));

        Assert.Equal(2, outcomes.Count(o => o.Succeeded));
        Assert.All(outcomes.Where(o => !o.Succeeded), o => Assert.Equal("Inventory.Stock.Insufficient", o.ErrorCode));
        Assert.Equal((5m, 4m), await AvailabilityAsync(room));
        await AssertReservationsReconcileAsync(room);
    }

    [Fact]
    public async Task A_reservation_racing_an_ordinary_issue_for_the_last_units_lets_only_one_take_them()
    {
        for (var round = 0; round < 3; round++)
        {
            var client = Admin();
            var room = await CreateStockroomAsync(fx, client);
            await ReceiveAsync(client, room, 3m, 1m);
            var issue = await DraftIdAsync(client, StockDocumentType.Issue, room.WarehouseId, null, Line(room.ProductId, room.Piece, 3m));
            var rowVersion = (await DocumentAsync(client, issue)).GetProperty("rowVersion").GetString();

            var reserving = ReserveAsync(room, Guid.NewGuid(), (Guid.NewGuid(), 3m));
            var issuing = ActAsync(Admin(), issue, "post", rowVersion: rowVersion);
            await Task.WhenAll(reserving, issuing);

            var reserved = (await reserving).Succeeded;
            var issued = (await issuing).StatusCode == HttpStatusCode.OK;
            Assert.True(reserved != issued, $"reserved {reserved}, issued {issued}");
            if (issued)
                Assert.Equal("Inventory.Stock.Insufficient", (await reserving).ErrorCode);
            else
                await (await issuing).AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.Stock.Insufficient");
            Assert.Equal(reserved ? (3m, 3m) : (0m, 0m), await AvailabilityAsync(room));
        }
    }

    [Fact]
    public async Task A_release_gives_back_only_what_the_reservation_still_holds_and_only_once()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        await ReceiveAsync(client, room, 10m, 1m);
        var (order, line) = (Guid.NewGuid(), Guid.NewGuid());
        Assert.True((await ReserveAsync(room, order, (line, 6m))).Succeeded);

        var partial = await ReleaseAsync(order, (line, 2m));
        var tooMuch = await ReleaseAsync(order, (line, 5m));
        var rest = await ReleaseAsync(order, (line, null));
        var again = await ReleaseAsync(order, (line, null));
        var otherOrder = await ReleaseAsync(Guid.NewGuid(), (line, null));

        Assert.Equal(2m, partial.Lines.Single().BaseQuantity);
        Assert.Equal("Inventory.Reservation.Exceeded", tooMuch.ErrorCode);
        Assert.Equal(4m, rest.Lines.Single().BaseQuantity);
        Assert.Equal(0m, again.Lines.Single().BaseQuantity);
        Assert.Equal("Inventory.Reservation.NotFound", otherOrder.ErrorCode);
        Assert.Equal((10m, 0m), await AvailabilityAsync(room));
        var reservation = await ReservationAsync(line);
        Assert.Equal((6m, 0m, 6m), (reservation.ReservedQuantity, reservation.ConsumedQuantity, reservation.ReleasedQuantity));
        Assert.Equal([ReservationEntryKind.Reserved, ReservationEntryKind.Released, ReservationEntryKind.Released],
            reservation.Entries.OrderBy(e => e.Id).Select(e => e.Kind));
        Assert.Equal("Inventory.Reservation.AlreadyReserved", (await ReserveAsync(room, order, (line, 1m))).ErrorCode);
    }

    [Fact]
    public async Task A_delivery_consumes_its_reservation_and_needs_the_stock_in_each_picked_location()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        var shelf = await AddLocationAsync(client, room.WarehouseId);
        await ReceiveAsync(client, room, 4m, 5m, shelf);
        await ReceiveAsync(client, room, 2m, 5m);
        var (order, line) = (Guid.NewGuid(), Guid.NewGuid());
        Assert.True((await ReserveAsync(room, order, (line, 6m))).Succeeded);

        var shortAtShelf = await DeliverAsync(room, order, (line, 5m, shelf));
        var unreserved = await DeliverAsync(room, Guid.NewGuid(), (line, 1m, shelf));
        var delivered = await DeliverAsync(room, order, (line, 4m, shelf), (line, 2m, room.DefaultLocationId));
        var beyond = await DeliverAsync(room, order, (line, 1m, room.DefaultLocationId));

        Assert.Equal("Inventory.Stock.InsufficientAtLocation", shortAtShelf.ErrorCode);
        Assert.Equal("Inventory.Reservation.NotFound", unreserved.ErrorCode);
        Assert.True(delivered.Succeeded, delivered.ErrorMessage);
        Assert.StartsWith("SDL-", delivered.Number);
        Assert.Equal([20m, 10m], delivered.Lines.Select(l => l.CostAmount));
        Assert.Equal("Inventory.Reservation.Exceeded", beyond.ErrorCode);
        Assert.Equal((0m, 0m), await AvailabilityAsync(room));
        var reservation = await ReservationAsync(line);
        Assert.Equal((6m, 0m), (reservation.ConsumedQuantity, reservation.RemainingQuantity));
        Assert.All(reservation.Entries.Where(e => e.Kind == ReservationEntryKind.Consumed), e => Assert.Equal(delivered.DocumentId, e.StockDocumentId));
        await AssertValuedAsync(client, room.ProductId, 0m, 0m);
        await AssertReservationsReconcileAsync(room);
    }

    [Fact]
    public async Task A_loss_that_over_reserves_leaves_every_reservation_alone_and_deliveries_still_need_the_stock()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        await ReceiveAsync(client, room, 4m, 1m);
        var (orderA, lineA, orderB, lineB) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Assert.True((await ReserveAsync(room, orderA, (lineA, 2m))).Succeeded);
        Assert.True((await ReserveAsync(room, orderB, (lineB, 2m))).Succeeded);

        var loss = await DraftIdAsync(client, StockDocumentType.Adjustment, room.WarehouseId, AdjustmentReason.Loss,
            Line(room.ProductId, room.Piece, 3m, direction: StockDirection.Out));
        Assert.Equal(HttpStatusCode.OK, (await ActAsync(client, loss, "submit")).StatusCode);
        var approved = await ActAsync(client, loss, "approve").PostedAsync();
        Assert.Equal(1, approved.GetProperty("overReservations").GetArrayLength());

        var more = await ReserveAsync(room, Guid.NewGuid(), (Guid.NewGuid(), 1m));
        var tooMuch = await DeliverAsync(room, orderA, (lineA, 2m, room.DefaultLocationId));
        var whatIsThere = await DeliverAsync(room, orderA, (lineA, 1m, room.DefaultLocationId));

        Assert.Equal("Inventory.Stock.Insufficient", more.ErrorCode);
        Assert.Equal("Inventory.Stock.Insufficient", tooMuch.ErrorCode);
        Assert.True(whatIsThere.Succeeded, whatIsThere.ErrorMessage);
        Assert.Equal((0m, 3m), await AvailabilityAsync(room));
        Assert.Equal(1m, (await ReservationAsync(lineA)).RemainingQuantity);
        Assert.Equal(2m, (await ReservationAsync(lineB)).RemainingQuantity);
        await AssertReservationsReconcileAsync(room);
    }

    [Fact]
    public async Task A_customer_return_comes_back_at_its_delivery_cost_never_twice()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        await ReceiveAsync(client, room, 3m, 10m);
        var (order, line) = (Guid.NewGuid(), Guid.NewGuid());
        Assert.True((await ReserveAsync(room, order, (line, 3m))).Succeeded);
        var delivered = await DeliverAsync(room, order, (line, 3m, room.DefaultLocationId));
        var deliveredLine = delivered.Lines.Single().StockLineId;
        await ReceiveAsync(client, room, 1m, 30m); // the average is now 30: returns must not come back at it

        var first = await ReturnAsync(room, (deliveredLine, 1m));
        var rest = await ReturnAsync(room, (deliveredLine, 2m));
        var beyond = await ReturnAsync(room, (deliveredLine, 1m));
        var notDelivered = await ReturnAsync(room, (Guid.NewGuid(), 1m));

        Assert.True(first.Succeeded, first.ErrorMessage);
        Assert.StartsWith("SRT-", first.Number);
        Assert.Equal(10m, first.Lines.Single().CostAmount);
        Assert.Equal(20m, rest.Lines.Single().CostAmount);
        Assert.Equal("Inventory.StockDocument.ReturnExceedsDelivery", beyond.ErrorCode);
        Assert.Equal("Inventory.StockDocument.DeliveryLineNotFound", notDelivered.ErrorCode);
        await AssertValuedAsync(client, room.ProductId, 4m, 60m);
        Assert.Equal((4m, 0m), await AvailabilityAsync(room));
    }

    [Fact]
    public async Task Inventory_endpoints_never_draft_or_reverse_sales_stock_documents_and_list_reservations_read_only()
    {
        var client = Admin();
        var room = await CreateStockroomAsync(fx, client);
        await ReceiveAsync(client, room, 2m, 1m);
        var (order, line) = (Guid.NewGuid(), Guid.NewGuid());
        Assert.True((await ReserveAsync(room, order, (line, 2m))).Succeeded);
        var delivered = await DeliverAsync(room, order, (line, 1m, room.DefaultLocationId));

        await (await DraftAsync(client, StockDocumentType.SalesDelivery, room.WarehouseId, null, Line(room.ProductId, room.Piece, 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.ManagedBySales");
        await (await client.PostJsonAsync($"{Documents}/{delivered.DocumentId}/reverse", new { notes = (string?)null }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Inventory.StockDocument.ManagedBySales");

        var listed = (await client.GetJsonAsync($"/api/inventory/reservations?warehouseId={room.WarehouseId}"))
            .GetProperty("items").EnumerateArray().Single();
        Assert.Equal(line, listed.GetProperty("sourceLineId").GetGuid());
        Assert.Equal((2m, 1m, 1m), (listed.GetProperty("reservedQuantity").GetDecimal(), listed.GetProperty("consumedQuantity").GetDecimal(),
            listed.GetProperty("remainingQuantity").GetDecimal()));
        await (await _api.CreateClient().As(InventoryPermissions.StockRead, Guid.NewGuid().ToString())
                .GetAsync($"/api/inventory/reservations?warehouseId={room.WarehouseId}"))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Inventory.Warehouse.AccessDenied");
    }

    // ------------------------------------------------------------------ the contracts, called the way Sales calls them

    private Task<StockReservationOutcome> ReserveAsync(Stockroom room, Guid orderId, params (Guid Line, decimal Pieces)[] lines) =>
        RunAsync(s => s.GetRequiredService<IStockReservations>().ReserveAsync(new StockReservationRequest(
            orderId, "SO-TEST", room.WarehouseId,
            lines.Select(l => new StockReservationLine(l.Line, room.ProductId, room.Piece, l.Pieces)).ToList()), CancellationToken.None),
            o => o.Succeeded);

    private Task<StockReservationOutcome> ReleaseAsync(Guid orderId, params (Guid Line, decimal? BaseQuantity)[] lines) =>
        RunAsync(s => s.GetRequiredService<IStockReservations>().ReleaseAsync(new StockReleaseRequest(
            orderId, "Test", lines.Select(l => new StockReleaseLine(l.Line, l.BaseQuantity)).ToList()), CancellationToken.None),
            o => o.Succeeded);

    private Task<SalesStockOutcome> DeliverAsync(Stockroom room, Guid orderId, params (Guid Line, decimal Pieces, Guid Location)[] lines) =>
        RunAsync(s => s.GetRequiredService<ISalesStockPosting>().PostDeliveryAsync(new SalesDeliveryStockRequest(
            Guid.NewGuid(), "DN-TEST", room.WarehouseId, orderId, null,
            lines.Select(l => new SalesDeliveryStockLine(l.Line, room.ProductId, room.Piece, l.Pieces, l.Location)).ToList()), CancellationToken.None),
            o => o.Succeeded);

    private Task<SalesStockOutcome> ReturnAsync(Stockroom room, params (Guid DeliveredLine, decimal Pieces)[] lines) =>
        RunAsync(s => s.GetRequiredService<ISalesStockPosting>().PostReturnAsync(new SalesReturnStockRequest(
            Guid.NewGuid(), "SR-TEST", room.WarehouseId, null, null,
            lines.Select(l => new SalesReturnStockLine(l.DeliveredLine, room.ProductId, room.Piece, l.Pieces, room.DefaultLocationId)).ToList()),
            CancellationToken.None),
            o => o.Succeeded);

    // A scope of the real host, one transaction: saved when the outcome succeeded, rolled back otherwise.
    private async Task<T> RunAsync<T>(Func<IServiceProvider, Task<T>> work, Func<T, bool> succeeded)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var outcome = default(T)!;
        await services.GetRequiredService<ITransactionRunner>().RunAsync<bool>(async ct =>
        {
            outcome = await work(services);
            if (!succeeded(outcome))
                return Error.Conflict("Test.Refused", "Refused; rolled back.");
            await services.GetRequiredService<DbContext>().SaveChangesAsync(ct);
            return true;
        }, CancellationToken.None);
        return outcome;
    }

    private async Task<(decimal OnHand, decimal Reserved)> AvailabilityAsync(Stockroom room)
    {
        await using var db = fx.NewDb();
        var row = await db.Set<ProductWarehouseStock>().SingleAsync(s => s.ProductId == room.ProductId && s.WarehouseId == room.WarehouseId);
        return (row.OnHandSellable, row.ReservedQuantity);
    }

    private async Task<StockReservation> ReservationAsync(Guid lineId)
    {
        await using var db = fx.NewDb();
        return await db.Set<StockReservation>().Include(r => r.Entries).SingleAsync(r => r.SourceLineId == lineId);
    }

    // The warehouse's reserved quantity is what its reservations still hold, and the reconciliation report agrees.
    private async Task AssertReservationsReconcileAsync(Stockroom room)
    {
        await using (var db = fx.NewDb())
        {
            var held = await db.Set<StockReservation>().Where(r => r.ProductId == room.ProductId && r.WarehouseId == room.WarehouseId)
                .SumAsync(r => r.ReservedQuantity - r.ConsumedQuantity - r.ReleasedQuantity);
            Assert.Equal(held, (await AvailabilityAsync(room)).Reserved);
        }
        var report = await Admin().GetJsonAsync("/api/inventory/reconciliation");
        Assert.DoesNotContain(report.GetProperty("reservationDifferences").EnumerateArray(),
            d => d.GetProperty("productId").GetGuid() == room.ProductId);
        Assert.DoesNotContain(report.GetProperty("availabilityDifferences").EnumerateArray(),
            d => d.GetProperty("productId").GetGuid() == room.ProductId);
    }
}
