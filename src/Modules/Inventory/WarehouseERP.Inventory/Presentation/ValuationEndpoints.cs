using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Inventory.Application.Contracts.Valuation;
using WarehouseERP.Inventory.Application.Features.Valuation.ListCostEntries;
using WarehouseERP.Inventory.Application.Features.Valuation.ListInventoryValuation;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Presentation;

// The company-wide moving average and its history (no general ledger yet).
internal static class ValuationEndpoints
{
    public const string Route = "/api/inventory/valuation";

    public static IEndpointRouteBuilder MapValuationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Valuation").RequireAuthorization(InventoryPermissions.ValuationRead);

        group.MapGet("/", ListAsync)
            .WithName("ListInventoryValuation")
            .Produces<PagedResult<InventoryValuationResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{productId:guid}/entries", ListEntriesAsync)
            .WithName("ListInventoryCostEntries")
            .Produces<PagedResult<InventoryCostEntryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListInventoryValuationQuery, PagedResult<InventoryValuationResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? productId = null, bool includeZero = false) =>
        (await handler.HandleAsync(new ListInventoryValuationQuery(page, pageSize, productId, includeZero), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ListEntriesAsync(
        Guid productId, IQueryHandler<ListCostEntriesQuery, PagedResult<InventoryCostEntryResponse>> handler,
        CancellationToken ct, int page = 1, int? pageSize = null) =>
        (await handler.HandleAsync(new ListCostEntriesQuery(page, pageSize, productId), ct)).ToHttpResult(TypedResults.Ok);
}
