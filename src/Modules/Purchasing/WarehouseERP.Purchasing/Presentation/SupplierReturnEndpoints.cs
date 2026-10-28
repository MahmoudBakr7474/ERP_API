using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.ApproveSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.CancelSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.CreateSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.GetSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.ListSupplierReturns;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.RejectSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.SubmitSupplierReturn;
using WarehouseERP.Purchasing.Application.Features.SupplierReturns.UpdateSupplierReturn;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Presentation.Contracts.Documents;
using WarehouseERP.Purchasing.Presentation.Contracts.SupplierReturns;

namespace WarehouseERP.Purchasing.Presentation;

// Drafting, submitting and cancelling need purchasing.returns.manage; approving (which posts the stock issue, with an
// Idempotency-Key) and rejecting purchasing.documents.approve, the creator included.
internal static class SupplierReturnEndpoints
{
    public const string Route = "/api/purchasing/returns";

    public static IEndpointRouteBuilder MapSupplierReturnEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("SupplierReturns");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(PurchasingPermissions.DocumentsRead)
            .WithName("ListSupplierReturns")
            .Produces<PagedResult<SupplierReturnSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetSupplierReturnQuery, SupplierReturnDetailsResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetSupplierReturnQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PurchasingPermissions.DocumentsRead)
            .WithName("GetSupplierReturn")
            .Produces<SupplierReturnDetailsResponse>()
            .QueryProblems();

        group.MapPost("/", (CreateSupplierReturnCommand command, ICommandHandler<CreateSupplierReturnCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(PurchasingPermissions.ReturnsManage)
            .WithName("CreateSupplierReturn")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdateSupplierReturnRequest request, ICommandHandler<UpdateSupplierReturnCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(
                    new UpdateSupplierReturnCommand(id, request.Reason, request.Notes, request.Lines, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.ReturnsManage)
            .WithName("UpdateSupplierReturn")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/submit", (Guid id, PurchasingActionRequest request, ICommandHandler<SubmitSupplierReturnCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new SubmitSupplierReturnCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.ReturnsManage)
            .WithName("SubmitSupplierReturn")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/approve", ApproveAsync)
            .RequireAuthorization(PurchasingPermissions.DocumentsApprove)
            .WithName("ApproveSupplierReturn")
            .Produces<SupplierReturnPostedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/reject", (Guid id, PurchasingReasonRequest request, ICommandHandler<RejectSupplierReturnCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new RejectSupplierReturnCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.DocumentsApprove)
            .WithName("RejectSupplierReturn")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/cancel", (Guid id, PurchasingActionRequest request, ICommandHandler<CancelSupplierReturnCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CancelSupplierReturnCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.ReturnsManage)
            .WithName("CancelSupplierReturn")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListSupplierReturnsQuery, PagedResult<SupplierReturnSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? supplierId = null, Guid? purchaseOrderId = null, Guid? goodsReceiptId = null,
        SupplierReturnStatus? status = null, bool? withCreditClaim = null, string? search = null) =>
        (await handler.HandleAsync(
                new ListSupplierReturnsQuery(page, pageSize, supplierId, purchaseOrderId, goodsReceiptId, status, withCreditClaim, search), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ApproveAsync(
        Guid id, PurchasingActionRequest request, [FromHeader(Name = EndpointConventions.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<ApproveSupplierReturnCommand, SupplierReturnPostedResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new ApproveSupplierReturnCommand(id, request.RowVersion, idempotencyKey ?? Guid.Empty), ct))
            .ToHttpResult(TypedResults.Ok);
}
