using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Application.Features.Stock.ListOverReservations;
using WarehouseERP.Inventory.Application.Features.Stock.ListStockAvailability;
using WarehouseERP.Inventory.Application.Features.Stock.ListStockBalances;
using WarehouseERP.Inventory.Application.Features.Stock.ListStockMovements;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Presentation;

// Stock levels and the ledger, limited to the user's warehouses.
internal static class StockEndpoints
{
    public const string Route = "/api/inventory";

    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Stock").RequireAuthorization(InventoryPermissions.StockRead);

        group.MapGet("/balances", ListBalancesAsync)
            .WithName("ListStockBalances")
            .Produces<PagedResult<StockBalanceResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/availability", ListAvailabilityAsync)
            .WithName("ListStockAvailability")
            .Produces<PagedResult<StockAvailabilityResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/movements", ListMovementsAsync)
            .WithName("ListStockMovements")
            .Produces<PagedResult<StockMovementResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/over-reservations", ListOverReservationsAsync)
            .WithName("ListOverReservations")
            .Produces<PagedResult<OverReservationResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> ListBalancesAsync(
        IQueryHandler<ListStockBalancesQuery, PagedResult<StockBalanceResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, Guid? locationId = null, Guid? productId = null,
        bool includeZero = false) =>
        (await handler.HandleAsync(new ListStockBalancesQuery(page, pageSize, warehouseId, locationId, productId, includeZero), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ListAvailabilityAsync(
        IQueryHandler<ListStockAvailabilityQuery, PagedResult<StockAvailabilityResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, Guid? productId = null, bool includeZero = false) =>
        (await handler.HandleAsync(new ListStockAvailabilityQuery(page, pageSize, warehouseId, productId, includeZero), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ListMovementsAsync(
        IQueryHandler<ListStockMovementsQuery, PagedResult<StockMovementResponse>> handler, CancellationToken ct,
        Guid? productId = null, Guid? warehouseId = null, Guid? locationId = null, DateTime? fromUtc = null,
        DateTime? toUtc = null, int page = 1, int? pageSize = null) =>
        (await handler.HandleAsync(new ListStockMovementsQuery(
                page, pageSize, productId ?? Guid.Empty, warehouseId ?? Guid.Empty, locationId, fromUtc, toUtc), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ListOverReservationsAsync(
        IQueryHandler<ListOverReservationsQuery, PagedResult<OverReservationResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, Guid? productId = null) =>
        (await handler.HandleAsync(new ListOverReservationsQuery(page, pageSize, warehouseId, productId), ct))
            .ToHttpResult(TypedResults.Ok);
}
