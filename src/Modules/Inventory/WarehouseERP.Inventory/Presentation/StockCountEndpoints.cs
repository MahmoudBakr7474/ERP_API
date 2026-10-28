using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Inventory.Application.Contracts.Counts;
using WarehouseERP.Inventory.Application.Features.Counts.ApproveStockCount;
using WarehouseERP.Inventory.Application.Features.Counts.CancelStockCount;
using WarehouseERP.Inventory.Application.Features.Counts.CreateStockCount;
using WarehouseERP.Inventory.Application.Features.Counts.EnterStockCountLines;
using WarehouseERP.Inventory.Application.Features.Counts.GetStockCount;
using WarehouseERP.Inventory.Application.Features.Counts.ListStockCounts;
using WarehouseERP.Inventory.Application.Features.Counts.RejectStockCount;
using WarehouseERP.Inventory.Application.Features.Counts.StartStockCount;
using WarehouseERP.Inventory.Application.Features.Counts.SubmitStockCount;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Presentation.Contracts.Counts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Presentation;

// Stock counts of warehouses in the user's scope: start freezes, approval posts the differences and releases.
internal static class StockCountEndpoints
{
    public const string Route = "/api/inventory/counts";

    public static IEndpointRouteBuilder MapStockCountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("StockCounts");

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(InventoryPermissions.CountsManage)
            .WithName("CreateStockCount")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/", ListAsync)
            .RequireAuthorization(InventoryPermissions.DocumentsRead)
            .WithName("ListStockCounts")
            .Produces<PagedResult<StockCountSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(InventoryPermissions.DocumentsRead)
            .WithName("GetStockCount")
            .Produces<StockCountDetailsResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        MapAction(group.MapPost("/{id:guid}/start",
                (Guid id, StockCountActionRequest request, ICommandHandler<StartStockCountCommand, string> handler, CancellationToken ct) =>
                    RowVersionAsync(handler.HandleAsync(new StartStockCountCommand(id, request.RowVersion), ct))),
            "StartStockCount", InventoryPermissions.CountsManage);
        MapAction(group.MapPut("/{id:guid}/lines",
                (Guid id, EnterStockCountLinesRequest request, ICommandHandler<EnterStockCountLinesCommand, string> handler, CancellationToken ct) =>
                    RowVersionAsync(handler.HandleAsync(new EnterStockCountLinesCommand(id, request.Lines, request.RowVersion), ct))),
            "EnterStockCountLines", InventoryPermissions.CountsManage);
        MapAction(group.MapPost("/{id:guid}/submit",
                (Guid id, StockCountActionRequest request, ICommandHandler<SubmitStockCountCommand, string> handler, CancellationToken ct) =>
                    RowVersionAsync(handler.HandleAsync(new SubmitStockCountCommand(id, request.RowVersion), ct))),
            "SubmitStockCount", InventoryPermissions.CountsManage);
        MapAction(group.MapPost("/{id:guid}/reject",
                (Guid id, RejectStockCountRequest request, ICommandHandler<RejectStockCountCommand, string> handler, CancellationToken ct) =>
                    RowVersionAsync(handler.HandleAsync(new RejectStockCountCommand(id, request.Reason, request.RowVersion), ct))),
            "RejectStockCount", InventoryPermissions.DocumentsApprove);
        MapAction(group.MapPost("/{id:guid}/cancel",
                (Guid id, StockCountActionRequest request, ICommandHandler<CancelStockCountCommand, string> handler, CancellationToken ct) =>
                    RowVersionAsync(handler.HandleAsync(new CancelStockCountCommand(id, request.RowVersion), ct))),
            "CancelStockCount", InventoryPermissions.CountsManage);

        group.MapPost("/{id:guid}/approve", ApproveAsync)
            .RequireAuthorization(InventoryPermissions.DocumentsApprove)
            .WithName("ApproveStockCount")
            .Produces<StockCountApprovalResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static void MapAction(RouteHandlerBuilder endpoint, string name, string permission) =>
        endpoint
            .RequireAuthorization(permission)
            .WithName(name)
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    private static async Task<IResult> CreateAsync(
        CreateStockCountCommand command, ICommandHandler<CreateStockCountCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct))
            .ToHttpResult(id => TypedResults.Created($"{Route}/{id}", new CreatedResponse(id)));

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListStockCountsQuery, PagedResult<StockCountSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, StockCountStatus? status = null) =>
        (await handler.HandleAsync(new ListStockCountsQuery(page, pageSize, warehouseId, status), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> GetAsync(
        Guid id, IQueryHandler<GetStockCountQuery, StockCountDetailsResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetStockCountQuery(id), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ApproveAsync(
        Guid id, StockCountActionRequest request, [FromHeader(Name = StockDocumentEndpoints.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<ApproveStockCountCommand, StockCountApprovalResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new ApproveStockCountCommand(id, idempotencyKey ?? Guid.Empty, request.RowVersion), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> RowVersionAsync(Task<Result<string>> pending) =>
        (await pending).ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));
}
