using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Pos.Application.Contracts.Returns;
using WarehouseERP.Pos.Application.Features.Returns.CreatePosReturn;
using WarehouseERP.Pos.Application.Features.Returns.GetPosReturn;
using WarehouseERP.Pos.Application.Features.Returns.ListPosReturns;
using WarehouseERP.Pos.Application.Features.Returns.ReprintPosReturn;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Presentation.Contracts.Returns;

namespace WarehouseERP.Pos.Presentation;

// Taking goods back and refunding them needs pos.returns.manage (with an Idempotency-Key, on the caller's own open shift;
// one's own sales included, no second person); reading pos.read; printing a copy pos.receipts.reprint.
internal static class PosReturnEndpoints
{
    public const string Route = "/api/pos/returns";

    public static IEndpointRouteBuilder MapPosReturnEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("PosReturns");

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(PosPermissions.ReturnsManage)
            .WithName("CreatePosReturn")
            .Produces<PosReturnResponse>()
            .CommandProblems();

        group.MapGet("/", ListAsync)
            .RequireAuthorization(PosPermissions.Read)
            .WithName("ListPosReturns")
            .Produces<PagedResult<PosReturnSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetPosReturnQuery, PosReturnResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetPosReturnQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PosPermissions.Read)
            .WithName("GetPosReturn")
            .Produces<PosReturnResponse>()
            .QueryProblems();

        group.MapPost("/{id:guid}/reprint", async (Guid id, ICommandHandler<ReprintPosReturnCommand, PosReturnResponse> handler,
                    CancellationToken ct) =>
                (await handler.HandleAsync(new ReprintPosReturnCommand(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PosPermissions.ReceiptsReprint)
            .WithName("ReprintPosReturn")
            .Produces<PosReturnResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreatePosReturnRequest request, [FromHeader(Name = EndpointConventions.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<CreatePosReturnCommand, PosReturnResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new CreatePosReturnCommand(
            request.TerminalId, request.SaleId, request.Reason, request.Notes, request.Lines, request.Refunds, idempotencyKey ?? Guid.Empty), ct))
        .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListPosReturnsQuery, PagedResult<PosReturnSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? terminalId = null, Guid? shiftId = null, Guid? saleId = null, Guid? warehouseId = null,
        string? search = null, DateOnly? from = null, DateOnly? to = null) =>
        (await handler.HandleAsync(new ListPosReturnsQuery(page, pageSize, terminalId, shiftId, saleId, warehouseId, search, from, to), ct))
            .ToHttpResult(TypedResults.Ok);
}
