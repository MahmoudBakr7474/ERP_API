using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Pos.Application.Contracts.CardIncidents;
using WarehouseERP.Pos.Application.Features.CardIncidents.ListPosCardIncidents;
using WarehouseERP.Pos.Application.Features.CardIncidents.ResolvePosCardIncident;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Presentation.Contracts.CardIncidents;

namespace WarehouseERP.Pos.Presentation;

// Card charges no sale was recorded for: listing needs pos.read, confirming one was reversed or settled pos.shifts.manage.
internal static class PosCardIncidentEndpoints
{
    public const string Route = "/api/pos/card-incidents";

    public static IEndpointRouteBuilder MapPosCardIncidentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("PosCardIncidents");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(PosPermissions.Read)
            .WithName("ListPosCardIncidents")
            .Produces<PagedResult<PosCardIncidentResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/{id:guid}/resolve", (Guid id, ResolvePosCardIncidentRequest request,
                    ICommandHandler<ResolvePosCardIncidentCommand, string> handler, CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new ResolvePosCardIncidentCommand(id, request.Note, request.RowVersion), ct)))
            .RequireAuthorization(PosPermissions.ShiftsManage)
            .WithName("ResolvePosCardIncident")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListPosCardIncidentsQuery, PagedResult<PosCardIncidentResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? terminalId = null, Guid? shiftId = null, PosCardIncidentStatus? status = null) =>
        (await handler.HandleAsync(new ListPosCardIncidentsQuery(page, pageSize, terminalId, shiftId, status), ct)).ToHttpResult(TypedResults.Ok);
}
