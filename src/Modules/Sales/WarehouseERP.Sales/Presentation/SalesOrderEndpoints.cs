using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Sales.Application.Contracts.Orders;
using WarehouseERP.Sales.Application.Features.Orders.ApproveSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.CancelSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.CloseSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.CreateSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.GetSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.ListSalesOrders;
using WarehouseERP.Sales.Application.Features.Orders.ReduceSalesOrderLine;
using WarehouseERP.Sales.Application.Features.Orders.RejectSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.SubmitSalesOrder;
using WarehouseERP.Sales.Application.Features.Orders.UpdateSalesOrder;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Presentation.Contracts.Documents;
using WarehouseERP.Sales.Presentation.Contracts.Orders;

namespace WarehouseERP.Sales.Presentation;

// Drafting, submitting (which confirms and reserves when no approval is needed), cancelling, closing and reducing lines
// need sales.orders.manage; approving and rejecting sales.documents.approve (the creator may approve their own order).
// The order's warehouse must be in the user's scope (checked by the handlers).
internal static class SalesOrderEndpoints
{
    public const string Route = "/api/sales/orders";

    public static IEndpointRouteBuilder MapSalesOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("SalesOrders");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("ListSalesOrders")
            .Produces<PagedResult<SalesOrderSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetSalesOrderQuery, SalesOrderDetailsResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetSalesOrderQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("GetSalesOrder")
            .Produces<SalesOrderDetailsResponse>()
            .QueryProblems();

        group.MapPost("/", (CreateSalesOrderCommand command, ICommandHandler<CreateSalesOrderCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(SalesPermissions.OrdersManage)
            .WithName("CreateSalesOrder")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdateSalesOrderRequest request, ICommandHandler<UpdateSalesOrderCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdateSalesOrderCommand(
                    id, request.CustomerId, request.OrderDate, request.RequestedDeliveryDate, request.CustomerReference, request.PaymentTermDays,
                    request.ShippingAddressId, request.Notes, request.Lines, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.OrdersManage)
            .WithName("UpdateSalesOrder")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/submit", (Guid id, SalesActionRequest request, ICommandHandler<SubmitSalesOrderCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new SubmitSalesOrderCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.OrdersManage)
            .WithName("SubmitSalesOrder")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/approve", (Guid id, SalesActionRequest request, ICommandHandler<ApproveSalesOrderCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new ApproveSalesOrderCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.DocumentsApprove)
            .WithName("ApproveSalesOrder")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/reject", (Guid id, SalesReasonRequest request, ICommandHandler<RejectSalesOrderCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new RejectSalesOrderCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.DocumentsApprove)
            .WithName("RejectSalesOrder")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/cancel", (Guid id, SalesReasonRequest request, ICommandHandler<CancelSalesOrderCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CancelSalesOrderCommand(id, request.Reason, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.OrdersManage)
            .WithName("CancelSalesOrder")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/close", (Guid id, SalesReasonRequest request, ICommandHandler<CloseSalesOrderCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CloseSalesOrderCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.OrdersManage)
            .WithName("CloseSalesOrder")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/lines/{lineId:guid}/reduce", (Guid id, Guid lineId, ReduceSalesOrderLineRequest request,
                    ICommandHandler<ReduceSalesOrderLineCommand, string> handler, CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new ReduceSalesOrderLineCommand(
                    id, lineId, request.Quantity, request.Reason, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.OrdersManage)
            .WithName("ReduceSalesOrderLine")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListSalesOrdersQuery, PagedResult<SalesOrderSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, Guid? customerId = null, SalesOrderStatus? status = null,
        string? search = null) =>
        (await handler.HandleAsync(new ListSalesOrdersQuery(page, pageSize, warehouseId, customerId, status, search), ct))
            .ToHttpResult(TypedResults.Ok);
}
