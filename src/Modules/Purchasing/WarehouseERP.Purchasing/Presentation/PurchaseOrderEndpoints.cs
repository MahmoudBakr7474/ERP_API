using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Purchasing.Application.Contracts.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Features.GoodsReceipts.ReceiveGoods;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ApprovePurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.CancelPurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ClosePurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.CreatePurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.GetPurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ListPurchaseOrders;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.RejectPurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.SubmitPurchaseOrder;
using WarehouseERP.Purchasing.Application.Features.PurchaseOrders.UpdatePurchaseOrder;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Presentation.Contracts.Documents;
using WarehouseERP.Purchasing.Presentation.Contracts.GoodsReceipts;
using WarehouseERP.Purchasing.Presentation.Contracts.PurchaseOrders;

namespace WarehouseERP.Purchasing.Presentation;

// Drafting, submitting, cancelling and closing need purchasing.orders.manage; approving and rejecting
// purchasing.documents.approve (the creator may approve their own order); receiving goods against an order
// purchasing.receipts.manage. The order's warehouse must be in the user's scope (checked by the handlers).
internal static class PurchaseOrderEndpoints
{
    public const string Route = "/api/purchasing/orders";

    public static IEndpointRouteBuilder MapPurchaseOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("PurchaseOrders");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(PurchasingPermissions.DocumentsRead)
            .WithName("ListPurchaseOrders")
            .Produces<PagedResult<PurchaseOrderSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetPurchaseOrderQuery, PurchaseOrderDetailsResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetPurchaseOrderQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PurchasingPermissions.DocumentsRead)
            .WithName("GetPurchaseOrder")
            .Produces<PurchaseOrderDetailsResponse>()
            .QueryProblems();

        group.MapPost("/", (CreatePurchaseOrderCommand command, ICommandHandler<CreatePurchaseOrderCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(PurchasingPermissions.OrdersManage)
            .WithName("CreatePurchaseOrder")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdatePurchaseOrderRequest request, ICommandHandler<UpdatePurchaseOrderCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdatePurchaseOrderCommand(
                    id, request.SupplierId, request.ExpectedDeliveryDate, request.SupplierReference, request.PaymentTermDays, request.Notes,
                    request.Lines, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.OrdersManage)
            .WithName("UpdatePurchaseOrder")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/submit", (Guid id, PurchasingActionRequest request, ICommandHandler<SubmitPurchaseOrderCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new SubmitPurchaseOrderCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.OrdersManage)
            .WithName("SubmitPurchaseOrder")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/approve", (Guid id, PurchasingActionRequest request, ICommandHandler<ApprovePurchaseOrderCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new ApprovePurchaseOrderCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.DocumentsApprove)
            .WithName("ApprovePurchaseOrder")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/reject", (Guid id, PurchasingReasonRequest request, ICommandHandler<RejectPurchaseOrderCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new RejectPurchaseOrderCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.DocumentsApprove)
            .WithName("RejectPurchaseOrder")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/cancel", (Guid id, PurchasingReasonRequest request, ICommandHandler<CancelPurchaseOrderCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CancelPurchaseOrderCommand(id, request.Reason, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.OrdersManage)
            .WithName("CancelPurchaseOrder")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/close", (Guid id, PurchasingReasonRequest request, ICommandHandler<ClosePurchaseOrderCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new ClosePurchaseOrderCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.OrdersManage)
            .WithName("ClosePurchaseOrder")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/receipts", ReceiveAsync)
            .RequireAuthorization(PurchasingPermissions.ReceiptsManage)
            .WithName("ReceiveGoods")
            .WithTags("GoodsReceipts")
            .Produces<GoodsReceiptPostedResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListPurchaseOrdersQuery, PagedResult<PurchaseOrderSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, Guid? supplierId = null, PurchaseOrderStatus? status = null,
        string? search = null) =>
        (await handler.HandleAsync(new ListPurchaseOrdersQuery(page, pageSize, warehouseId, supplierId, status, search), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ReceiveAsync(
        Guid id, ReceiveGoodsRequest request, [FromHeader(Name = EndpointConventions.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<ReceiveGoodsCommand, GoodsReceiptPostedResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(
                new ReceiveGoodsCommand(id, request.SupplierDeliveryNote, request.Notes, request.Lines, idempotencyKey ?? Guid.Empty), ct))
            .ToHttpResult(TypedResults.Ok);
}
