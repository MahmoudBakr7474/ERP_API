using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Purchasing.Application.Contracts.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Features.GoodsReceipts.GetGoodsReceipt;
using WarehouseERP.Purchasing.Application.Features.GoodsReceipts.ListGoodsReceipts;
using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Presentation;

// Posted goods receipts (receiving itself is POST /api/purchasing/orders/{id}/receipts).
internal static class GoodsReceiptEndpoints
{
    public const string Route = "/api/purchasing/receipts";

    public static IEndpointRouteBuilder MapGoodsReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("GoodsReceipts");

        group.MapGet("/", async (IQueryHandler<ListGoodsReceiptsQuery, PagedResult<GoodsReceiptSummaryResponse>> handler, CancellationToken ct,
                    int page = 1, int? pageSize = null, Guid? warehouseId = null, Guid? supplierId = null, Guid? purchaseOrderId = null,
                    string? search = null) =>
                (await handler.HandleAsync(new ListGoodsReceiptsQuery(page, pageSize, warehouseId, supplierId, purchaseOrderId, search), ct))
                .ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PurchasingPermissions.DocumentsRead)
            .WithName("ListGoodsReceipts")
            .Produces<PagedResult<GoodsReceiptSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetGoodsReceiptQuery, GoodsReceiptDetailsResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetGoodsReceiptQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PurchasingPermissions.DocumentsRead)
            .WithName("GetGoodsReceipt")
            .Produces<GoodsReceiptDetailsResponse>()
            .QueryProblems();

        return app;
    }
}
