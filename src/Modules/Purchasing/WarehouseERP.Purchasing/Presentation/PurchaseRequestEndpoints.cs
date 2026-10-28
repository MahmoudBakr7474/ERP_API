using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ApprovePurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.CancelPurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ClosePurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.CreatePurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.GetPurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ListPurchaseRequests;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.RejectPurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.SubmitPurchaseRequest;
using WarehouseERP.Purchasing.Application.Features.PurchaseRequests.UpdatePurchaseRequest;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Presentation.Contracts.Documents;
using WarehouseERP.Purchasing.Presentation.Contracts.PurchaseRequests;

namespace WarehouseERP.Purchasing.Presentation;

// Drafting, submitting, cancelling and closing need purchasing.requests.manage; approving and rejecting
// purchasing.documents.approve, the creator included. Orders draw on approved requests through their lines.
internal static class PurchaseRequestEndpoints
{
    public const string Route = "/api/purchasing/requests";

    public static IEndpointRouteBuilder MapPurchaseRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("PurchaseRequests");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(PurchasingPermissions.DocumentsRead)
            .WithName("ListPurchaseRequests")
            .Produces<PagedResult<PurchaseRequestSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetPurchaseRequestQuery, PurchaseRequestDetailsResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetPurchaseRequestQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PurchasingPermissions.DocumentsRead)
            .WithName("GetPurchaseRequest")
            .Produces<PurchaseRequestDetailsResponse>()
            .QueryProblems();

        group.MapPost("/", (CreatePurchaseRequestCommand command, ICommandHandler<CreatePurchaseRequestCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(PurchasingPermissions.RequestsManage)
            .WithName("CreatePurchaseRequest")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdatePurchaseRequestRequest request, ICommandHandler<UpdatePurchaseRequestCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(
                    new UpdatePurchaseRequestCommand(id, request.NeededByDate, request.Notes, request.Lines, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.RequestsManage)
            .WithName("UpdatePurchaseRequest")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/submit", (Guid id, PurchasingActionRequest request, ICommandHandler<SubmitPurchaseRequestCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new SubmitPurchaseRequestCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.RequestsManage)
            .WithName("SubmitPurchaseRequest")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/approve", (Guid id, PurchasingActionRequest request, ICommandHandler<ApprovePurchaseRequestCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new ApprovePurchaseRequestCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.DocumentsApprove)
            .WithName("ApprovePurchaseRequest")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/reject", (Guid id, PurchasingReasonRequest request, ICommandHandler<RejectPurchaseRequestCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new RejectPurchaseRequestCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.DocumentsApprove)
            .WithName("RejectPurchaseRequest")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/cancel", (Guid id, PurchasingActionRequest request, ICommandHandler<CancelPurchaseRequestCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CancelPurchaseRequestCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.RequestsManage)
            .WithName("CancelPurchaseRequest")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/close", (Guid id, PurchasingReasonRequest request, ICommandHandler<ClosePurchaseRequestCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new ClosePurchaseRequestCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.RequestsManage)
            .WithName("ClosePurchaseRequest")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListPurchaseRequestsQuery, PagedResult<PurchaseRequestSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, PurchaseRequestStatus? status = null, bool orderable = false,
        string? search = null) =>
        (await handler.HandleAsync(new ListPurchaseRequestsQuery(page, pageSize, warehouseId, status, orderable, search), ct))
            .ToHttpResult(TypedResults.Ok);
}
