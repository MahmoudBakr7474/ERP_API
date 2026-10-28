using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Inventory.Application.Contracts.Transfers;
using WarehouseERP.Inventory.Application.Features.Transfers.ApproveStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.CancelStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.CloseStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.CreateStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.DispatchStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.GetStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.ListStockTransfers;
using WarehouseERP.Inventory.Application.Features.Transfers.ListTransferDestinations;
using WarehouseERP.Inventory.Application.Features.Transfers.ReceiveStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.RejectStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.SubmitStockTransfer;
using WarehouseERP.Inventory.Application.Features.Transfers.UpdateStockTransfer;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Presentation.Contracts.Transfers;

namespace WarehouseERP.Inventory.Presentation;

// Transfers between warehouses through a transit warehouse. The source warehouse's people prepare and dispatch, the
// destination's receive; approving needs the approval permission; the handlers check which warehouse must be in scope.
// The destinations lookup lists every active physical warehouse: a sender may ship outside their own scope.
internal static class StockTransferEndpoints
{
    public const string Route = "/api/inventory/transfers";

    public static IEndpointRouteBuilder MapStockTransferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("StockTransfers");

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(InventoryPermissions.TransfersManage)
            .WithName("CreateStockTransfer")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/", ListAsync)
            .RequireAuthorization(InventoryPermissions.DocumentsRead)
            .WithName("ListStockTransfers")
            .Produces<PagedResult<StockTransferSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/destinations", ListDestinationsAsync)
            .RequireAuthorization(InventoryPermissions.TransfersManage)
            .WithName("ListTransferDestinations")
            .Produces<IReadOnlyList<TransferDestinationResponse>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(InventoryPermissions.DocumentsRead)
            .WithName("GetStockTransfer")
            .Produces<StockTransferDetailsResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(InventoryPermissions.TransfersManage)
            .WithName("UpdateStockTransfer")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        MapAction(group, "/{id:guid}/submit", "SubmitStockTransfer", InventoryPermissions.TransfersManage,
            (Guid id, StockTransferActionRequest request, ICommandHandler<SubmitStockTransferCommand, string> handler, CancellationToken ct) =>
                RowVersionAsync(handler.HandleAsync(new SubmitStockTransferCommand(id, request.RowVersion), ct)));
        MapAction(group, "/{id:guid}/reject", "RejectStockTransfer", InventoryPermissions.DocumentsApprove,
            (Guid id, RejectStockTransferRequest request, ICommandHandler<RejectStockTransferCommand, string> handler, CancellationToken ct) =>
                RowVersionAsync(handler.HandleAsync(new RejectStockTransferCommand(id, request.Reason, request.RowVersion), ct)));
        MapAction(group, "/{id:guid}/cancel", "CancelStockTransfer", InventoryPermissions.TransfersManage,
            (Guid id, StockTransferActionRequest request, ICommandHandler<CancelStockTransferCommand, string> handler, CancellationToken ct) =>
                RowVersionAsync(handler.HandleAsync(new CancelStockTransferCommand(id, request.RowVersion), ct)));

        group.MapPost("/{id:guid}/dispatch", DispatchAsync)
            .RequireAuthorization(InventoryPermissions.TransfersManage)
            .WithName("DispatchStockTransfer")
            .Produces<StockTransferPostedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/approve", ApproveAsync)
            .RequireAuthorization(InventoryPermissions.DocumentsApprove)
            .WithName("ApproveStockTransfer")
            .Produces<StockTransferPostedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/receive", ReceiveAsync)
            .RequireAuthorization(InventoryPermissions.TransfersReceive)
            .WithName("ReceiveStockTransfer")
            .Produces<StockTransferPostedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/close", CloseAsync)
            .RequireAuthorization(InventoryPermissions.TransfersManage)
            .WithName("CloseStockTransfer")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static void MapAction(RouteGroupBuilder group, string pattern, string name, string permission, Delegate handler) =>
        group.MapPost(pattern, handler)
            .RequireAuthorization(permission)
            .WithName(name)
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    private static async Task<IResult> CreateAsync(
        CreateStockTransferCommand command, ICommandHandler<CreateStockTransferCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct))
            .ToHttpResult(id => TypedResults.Created($"{Route}/{id}", new CreatedResponse(id)));

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListStockTransfersQuery, PagedResult<StockTransferSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, StockTransferStatus? status = null, string? search = null) =>
        (await handler.HandleAsync(new ListStockTransfersQuery(page, pageSize, warehouseId, status, search), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ListDestinationsAsync(
        IQueryHandler<ListTransferDestinationsQuery, IReadOnlyList<TransferDestinationResponse>> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new ListTransferDestinationsQuery(), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> GetAsync(
        Guid id, IQueryHandler<GetStockTransferQuery, StockTransferDetailsResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetStockTransferQuery(id), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateStockTransferRequest request, ICommandHandler<UpdateStockTransferCommand, string> handler, CancellationToken ct) =>
        await RowVersionAsync(handler.HandleAsync(new UpdateStockTransferCommand(
            id, request.TransitWarehouseId, request.ExternalReference, request.Notes, request.Lines, request.RowVersion), ct));

    private static async Task<IResult> DispatchAsync(
        Guid id, StockTransferActionRequest request, [FromHeader(Name = StockDocumentEndpoints.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<DispatchStockTransferCommand, StockTransferPostedResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new DispatchStockTransferCommand(id, idempotencyKey ?? Guid.Empty, request.RowVersion), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ApproveAsync(
        Guid id, StockTransferActionRequest request, [FromHeader(Name = StockDocumentEndpoints.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<ApproveStockTransferCommand, StockTransferPostedResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new ApproveStockTransferCommand(id, idempotencyKey ?? Guid.Empty, request.RowVersion), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ReceiveAsync(
        Guid id, ReceiveStockTransferRequest request, [FromHeader(Name = StockDocumentEndpoints.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<ReceiveStockTransferCommand, StockTransferPostedResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new ReceiveStockTransferCommand(id, idempotencyKey ?? Guid.Empty, request.Lines, request.RowVersion), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> CloseAsync(
        Guid id, CloseStockTransferRequest request, ICommandHandler<CloseStockTransferCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new CloseStockTransferCommand(id, request.Reason, request.Notes, request.RowVersion), ct))
            .ToHttpResult(documentId => TypedResults.Created($"{StockDocumentEndpoints.Route}/{documentId}", new CreatedResponse(documentId)));

    private static async Task<IResult> RowVersionAsync(Task<SharedKernel.Results.Result<string>> pending) =>
        (await pending).ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));
}
