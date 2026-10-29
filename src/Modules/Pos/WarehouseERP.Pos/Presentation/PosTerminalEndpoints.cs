using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Pos.Application.Contracts.Shifts;
using WarehouseERP.Pos.Application.Contracts.Terminals;
using WarehouseERP.Pos.Application.Features.Shifts.GetCurrentPosShift;
using WarehouseERP.Pos.Application.Features.Terminals.CreatePosTerminal;
using WarehouseERP.Pos.Application.Features.Terminals.GetPosTerminal;
using WarehouseERP.Pos.Application.Features.Terminals.ListPosTerminals;
using WarehouseERP.Pos.Application.Features.Terminals.SetPosTerminalActive;
using WarehouseERP.Pos.Application.Features.Terminals.UpdatePosTerminal;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Presentation.Contracts.Terminals;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Presentation;

// Terminals of the user's warehouses: reading needs pos.read, registering and configuring pos.terminals.manage. A cashier
// (pos.sell) asks a terminal for their own open shift there.
internal static class PosTerminalEndpoints
{
    public const string Route = "/api/pos/terminals";

    public static IEndpointRouteBuilder MapPosTerminalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("PosTerminals");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(PosPermissions.Read)
            .WithName("ListPosTerminals")
            .Produces<PagedResult<PosTerminalResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetPosTerminalQuery, PosTerminalResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetPosTerminalQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PosPermissions.Read)
            .WithName("GetPosTerminal")
            .Produces<PosTerminalResponse>()
            .QueryProblems();

        group.MapPost("/", (CreatePosTerminalCommand command, ICommandHandler<CreatePosTerminalCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(PosPermissions.TerminalsManage)
            .WithName("CreatePosTerminal")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdatePosTerminalRequest request, ICommandHandler<UpdatePosTerminalCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdatePosTerminalCommand(
                    id, request.Name, request.SellingLocationId, request.AcceptsCash, request.AcceptsCard, request.ReceiptHeader,
                    request.ReceiptFooter, request.RowVersion), ct)))
            .RequireAuthorization(PosPermissions.TerminalsManage)
            .WithName("UpdatePosTerminal")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/activate", (Guid id, ICommandHandler<SetPosTerminalActiveCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new SetPosTerminalActiveCommand(id, IsActive: true), ct)))
            .RequireAuthorization(PosPermissions.TerminalsManage)
            .WithName("ActivatePosTerminal")
            .Produces(StatusCodes.Status204NoContent)
            .CommandProblems();

        group.MapPost("/{id:guid}/deactivate", (Guid id, ICommandHandler<SetPosTerminalActiveCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new SetPosTerminalActiveCommand(id, IsActive: false), ct)))
            .RequireAuthorization(PosPermissions.TerminalsManage)
            .WithName("DeactivatePosTerminal")
            .Produces(StatusCodes.Status204NoContent)
            .CommandProblems();

        group.MapGet("/{id:guid}/current-shift", async (Guid id, IQueryHandler<GetCurrentPosShiftQuery, PosShiftResponse> handler,
                    CancellationToken ct) =>
                (await handler.HandleAsync(new GetCurrentPosShiftQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PosPermissions.Sell)
            .WithName("GetCurrentPosShift")
            .Produces<PosShiftResponse>()
            .QueryProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListPosTerminalsQuery, PagedResult<PosTerminalResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? warehouseId = null, bool? isActive = null, string? search = null) =>
        (await handler.HandleAsync(new ListPosTerminalsQuery(page, pageSize, warehouseId, isActive, search), ct)).ToHttpResult(TypedResults.Ok);
}
