using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Pos.Application.Contracts.Shifts;
using WarehouseERP.Pos.Application.Features.Shifts.ClosePosShift;
using WarehouseERP.Pos.Application.Features.Shifts.GetPosShift;
using WarehouseERP.Pos.Application.Features.Shifts.ListPosShifts;
using WarehouseERP.Pos.Application.Features.Shifts.OpenPosShift;
using WarehouseERP.Pos.Application.Features.Shifts.RecordPosCashMovement;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Presentation.Contracts.Shifts;

namespace WarehouseERP.Pos.Presentation;

// Opening one's own shift needs pos.sell; cash in and out pos.cash.manage (the shift's cashier, or a shift manager);
// closing is open to any signed-in user and decided by the handler: the cashier with pos.sell, anyone else with
// pos.shifts.manage (a forced close with a note). Shift reports need pos.read. The terminal's warehouse must be in scope.
internal static class PosShiftEndpoints
{
    public const string Route = "/api/pos/shifts";

    public static IEndpointRouteBuilder MapPosShiftEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("PosShifts");

        group.MapPost("/", (OpenPosShiftCommand command, ICommandHandler<OpenPosShiftCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(PosPermissions.Sell)
            .WithName("OpenPosShift")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapGet("/", ListAsync)
            .RequireAuthorization(PosPermissions.Read)
            .WithName("ListPosShifts")
            .Produces<PagedResult<PosShiftSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetPosShiftQuery, PosShiftResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetPosShiftQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PosPermissions.Read)
            .WithName("GetPosShift")
            .Produces<PosShiftResponse>()
            .QueryProblems();

        group.MapPost("/{id:guid}/cash-movements", (Guid id, RecordPosCashMovementRequest request,
                    ICommandHandler<RecordPosCashMovementCommand, string> handler, CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new RecordPosCashMovementCommand(
                    id, request.MovementType, request.Amount, request.Reason, request.RowVersion), ct)))
            .RequireAuthorization(PosPermissions.CashManage)
            .WithName("RecordPosCashMovement")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/close", async (Guid id, ClosePosShiftRequest request, ICommandHandler<ClosePosShiftCommand, PosShiftResponse> handler,
                    CancellationToken ct) =>
                (await handler.HandleAsync(new ClosePosShiftCommand(id, request.CountedCash, request.Note, request.RowVersion), ct))
                .ToHttpResult(TypedResults.Ok))
            .RequireAuthorization()
            .WithName("ClosePosShift")
            .Produces<PosShiftResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListPosShiftsQuery, PagedResult<PosShiftSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? terminalId = null, Guid? warehouseId = null, Guid? cashierUserId = null,
        PosShiftStatus? status = null) =>
        (await handler.HandleAsync(new ListPosShiftsQuery(page, pageSize, terminalId, warehouseId, cashierUserId, status), ct))
            .ToHttpResult(TypedResults.Ok);
}
