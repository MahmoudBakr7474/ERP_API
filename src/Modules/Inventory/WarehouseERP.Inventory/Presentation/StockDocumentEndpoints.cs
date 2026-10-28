using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Features.StockDocuments.ApproveStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.CancelStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.CreateStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.GetStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.ListStockDocuments;
using WarehouseERP.Inventory.Application.Features.StockDocuments.PostStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.RejectStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.ReverseStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.SubmitStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.UpdateStockDocument;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Presentation.Contracts.StockDocuments;

namespace WarehouseERP.Inventory.Presentation;

// HTTP only: bind the request, map it to a command or query, call the handler pipeline and map the Result. Drafting,
// submitting, posting, cancelling and reversing need the manage permission of the document's type, which the handler
// checks (the type is in the body or the document); reading and approving have fixed permissions.
internal static class StockDocumentEndpoints
{
    public const string Route = "/api/inventory/documents";

    /// <summary>A new GUID per posting or approval click, sent again unchanged when the client retries it.</summary>
    public const string IdempotencyHeader = "Idempotency-Key";

    public static IEndpointRouteBuilder MapStockDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("StockDocuments");

        group.MapPost("/", CreateAsync)
            .RequireAuthorization()
            .WithName("CreateStockDocument")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/", ListAsync)
            .RequireAuthorization(InventoryPermissions.DocumentsRead)
            .WithName("ListStockDocuments")
            .Produces<PagedResult<StockDocumentSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(InventoryPermissions.DocumentsRead)
            .WithName("GetStockDocument")
            .Produces<StockDocumentDetailsResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization()
            .WithName("UpdateStockDocument")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/submit", SubmitAsync)
            .RequireAuthorization()
            .WithName("SubmitStockDocument")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/post", PostAsync)
            .RequireAuthorization()
            .WithName("PostStockDocument")
            .Produces<PostedDocumentResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/approve", ApproveAsync)
            .RequireAuthorization(InventoryPermissions.DocumentsApprove)
            .WithName("ApproveStockDocument")
            .Produces<PostedDocumentResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/reject", RejectAsync)
            .RequireAuthorization(InventoryPermissions.DocumentsApprove)
            .WithName("RejectStockDocument")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/cancel", CancelAsync)
            .RequireAuthorization()
            .WithName("CancelStockDocument")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/reverse", ReverseAsync)
            .RequireAuthorization()
            .WithName("ReverseStockDocument")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateStockDocumentCommand command, ICommandHandler<CreateStockDocumentCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct))
            .ToHttpResult(id => TypedResults.Created($"{Route}/{id}", new CreatedResponse(id)));

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListStockDocumentsQuery, PagedResult<StockDocumentSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, StockDocumentType? documentType = null,
        StockDocumentStatus? status = null, string? search = null) =>
        (await handler.HandleAsync(new ListStockDocumentsQuery(page, pageSize, warehouseId, documentType, status, search), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> GetAsync(
        Guid id, IQueryHandler<GetStockDocumentQuery, StockDocumentDetailsResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetStockDocumentQuery(id), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateStockDocumentRequest request, ICommandHandler<UpdateStockDocumentCommand, string> handler, CancellationToken ct) =>
        (await handler.HandleAsync(
                new UpdateStockDocumentCommand(id, request.AdjustmentReason, request.ExternalReference, request.Notes, request.Lines, request.RowVersion), ct))
            .ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));

    private static async Task<IResult> SubmitAsync(
        Guid id, StockDocumentActionRequest request, ICommandHandler<SubmitStockDocumentCommand, string> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new SubmitStockDocumentCommand(id, request.RowVersion), ct))
            .ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));

    private static async Task<IResult> PostAsync(
        Guid id, StockDocumentActionRequest request, [FromHeader(Name = IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<PostStockDocumentCommand, PostedDocumentResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new PostStockDocumentCommand(id, idempotencyKey ?? Guid.Empty, request.RowVersion), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ApproveAsync(
        Guid id, StockDocumentActionRequest request, [FromHeader(Name = IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<ApproveStockDocumentCommand, PostedDocumentResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new ApproveStockDocumentCommand(id, idempotencyKey ?? Guid.Empty, request.RowVersion), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> RejectAsync(
        Guid id, RejectStockDocumentRequest request, ICommandHandler<RejectStockDocumentCommand, string> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new RejectStockDocumentCommand(id, request.Reason, request.RowVersion), ct))
            .ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));

    private static async Task<IResult> CancelAsync(
        Guid id, StockDocumentActionRequest request, ICommandHandler<CancelStockDocumentCommand, string> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new CancelStockDocumentCommand(id, request.RowVersion), ct))
            .ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));

    private static async Task<IResult> ReverseAsync(
        Guid id, ReverseStockDocumentRequest request, ICommandHandler<ReverseStockDocumentCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new ReverseStockDocumentCommand(id, request.Notes), ct))
            .ToHttpResult(reversalId => TypedResults.Created($"{Route}/{reversalId}", new CreatedResponse(reversalId)));
}
