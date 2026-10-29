using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Sales.Application.Contracts.Deliveries;
using WarehouseERP.Sales.Application.Features.Deliveries.CancelSalesDelivery;
using WarehouseERP.Sales.Application.Features.Deliveries.CreateSalesDelivery;
using WarehouseERP.Sales.Application.Features.Deliveries.GetSalesDelivery;
using WarehouseERP.Sales.Application.Features.Deliveries.ListSalesDeliveries;
using WarehouseERP.Sales.Application.Features.Deliveries.PostSalesDelivery;
using WarehouseERP.Sales.Application.Features.Deliveries.UpdateSalesDelivery;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Presentation.Contracts.Deliveries;
using WarehouseERP.Sales.Presentation.Contracts.Documents;

namespace WarehouseERP.Sales.Presentation;

// Picking lists and their posting need sales.deliveries.manage; posting takes an Idempotency-Key. The order's warehouse
// must be in the user's scope (checked by the handlers).
internal static class SalesDeliveryEndpoints
{
    public const string Route = "/api/sales/deliveries";

    public static IEndpointRouteBuilder MapSalesDeliveryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("SalesDeliveries");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("ListSalesDeliveries")
            .Produces<PagedResult<SalesDeliverySummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetSalesDeliveryQuery, SalesDeliveryDetailsResponse> handler,
                    CancellationToken ct) =>
                (await handler.HandleAsync(new GetSalesDeliveryQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("GetSalesDelivery")
            .Produces<SalesDeliveryDetailsResponse>()
            .QueryProblems();

        group.MapPost("/", (CreateSalesDeliveryCommand command, ICommandHandler<CreateSalesDeliveryCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(SalesPermissions.DeliveriesManage)
            .WithName("CreateSalesDelivery")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdateSalesDeliveryRequest request, ICommandHandler<UpdateSalesDeliveryCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdateSalesDeliveryCommand(
                    id, request.Notes, request.Lines, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.DeliveriesManage)
            .WithName("UpdateSalesDelivery")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/cancel", (Guid id, SalesActionRequest request, ICommandHandler<CancelSalesDeliveryCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CancelSalesDeliveryCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.DeliveriesManage)
            .WithName("CancelSalesDelivery")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/post", PostAsync)
            .RequireAuthorization(SalesPermissions.DeliveriesManage)
            .WithName("PostSalesDelivery")
            .Produces<SalesDeliveryPostedResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListSalesDeliveriesQuery, PagedResult<SalesDeliverySummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, Guid? orderId = null, Guid? customerId = null,
        SalesDeliveryStatus? status = null, string? search = null) =>
        (await handler.HandleAsync(new ListSalesDeliveriesQuery(page, pageSize, warehouseId, orderId, customerId, status, search), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> PostAsync(
        Guid id, SalesActionRequest request, [FromHeader(Name = EndpointConventions.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<PostSalesDeliveryCommand, SalesDeliveryPostedResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new PostSalesDeliveryCommand(id, request.RowVersion, idempotencyKey ?? Guid.Empty), ct))
            .ToHttpResult(TypedResults.Ok);
}
