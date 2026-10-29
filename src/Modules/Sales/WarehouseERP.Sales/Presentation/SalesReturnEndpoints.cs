using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Sales.Application.Contracts.Returns;
using WarehouseERP.Sales.Application.Features.Returns.ApproveSalesReturn;
using WarehouseERP.Sales.Application.Features.Returns.CancelSalesReturn;
using WarehouseERP.Sales.Application.Features.Returns.CreateSalesReturn;
using WarehouseERP.Sales.Application.Features.Returns.GetSalesReturn;
using WarehouseERP.Sales.Application.Features.Returns.ListSalesReturns;
using WarehouseERP.Sales.Application.Features.Returns.RejectSalesReturn;
using WarehouseERP.Sales.Application.Features.Returns.SubmitSalesReturn;
using WarehouseERP.Sales.Application.Features.Returns.UpdateSalesReturn;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Presentation.Contracts.Documents;
using WarehouseERP.Sales.Presentation.Contracts.Returns;

namespace WarehouseERP.Sales.Presentation;

// Drafting, submitting and cancelling customer returns need sales.returns.manage; approving (which restocks, with an
// Idempotency-Key) and rejecting sales.documents.approve, the creator included. The delivery's warehouse must be in the
// user's scope (checked by the handlers).
internal static class SalesReturnEndpoints
{
    public const string Route = "/api/sales/returns";

    public static IEndpointRouteBuilder MapSalesReturnEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("SalesReturns");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("ListSalesReturns")
            .Produces<PagedResult<SalesReturnSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetSalesReturnQuery, SalesReturnDetailsResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetSalesReturnQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("GetSalesReturn")
            .Produces<SalesReturnDetailsResponse>()
            .QueryProblems();

        group.MapPost("/", (CreateSalesReturnCommand command, ICommandHandler<CreateSalesReturnCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(SalesPermissions.ReturnsManage)
            .WithName("CreateSalesReturn")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdateSalesReturnRequest request, ICommandHandler<UpdateSalesReturnCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdateSalesReturnCommand(
                    id, request.Reason, request.Notes, request.Lines, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.ReturnsManage)
            .WithName("UpdateSalesReturn")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/submit", (Guid id, SalesActionRequest request, ICommandHandler<SubmitSalesReturnCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new SubmitSalesReturnCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.ReturnsManage)
            .WithName("SubmitSalesReturn")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/approve", ApproveAsync)
            .RequireAuthorization(SalesPermissions.DocumentsApprove)
            .WithName("ApproveSalesReturn")
            .Produces<SalesReturnApprovedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/reject", (Guid id, SalesReasonRequest request, ICommandHandler<RejectSalesReturnCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new RejectSalesReturnCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.DocumentsApprove)
            .WithName("RejectSalesReturn")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/cancel", (Guid id, SalesActionRequest request, ICommandHandler<CancelSalesReturnCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CancelSalesReturnCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(SalesPermissions.ReturnsManage)
            .WithName("CancelSalesReturn")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListSalesReturnsQuery, PagedResult<SalesReturnSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, Guid? orderId = null, Guid? deliveryId = null, Guid? customerId = null,
        SalesReturnStatus? status = null, string? search = null) =>
        (await handler.HandleAsync(new ListSalesReturnsQuery(page, pageSize, warehouseId, orderId, deliveryId, customerId, status, search), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ApproveAsync(
        Guid id, SalesActionRequest request, [FromHeader(Name = EndpointConventions.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<ApproveSalesReturnCommand, SalesReturnApprovedResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new ApproveSalesReturnCommand(id, request.RowVersion, idempotencyKey ?? Guid.Empty), ct))
            .ToHttpResult(TypedResults.Ok);
}
