using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Catalog.Application.Contracts.Units;
using WarehouseERP.Catalog.Application.Features.Units.CreateUnit;
using WarehouseERP.Catalog.Application.Features.Units.GetUnit;
using WarehouseERP.Catalog.Application.Features.Units.ListUnits;
using WarehouseERP.Catalog.Application.Features.Units.SetUnitActive;
using WarehouseERP.Catalog.Application.Features.Units.UpdateUnit;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Presentation.Contracts.Units;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Presentation;

// HTTP only: bind the request, map it to a command or query, call the handler pipeline and map the Result.
// Anyone who reads products reads units; changes need catalog.units.manage.
internal static class UnitEndpoints
{
    public const string Route = "/api/catalog/units";

    public static IEndpointRouteBuilder MapUnitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Units");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(CatalogPermissions.ProductsRead)
            .WithName("ListUnits")
            .Produces<PagedResult<UnitSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(CatalogPermissions.ProductsRead)
            .WithName("GetUnit")
            .Produces<UnitDetailsResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(CatalogPermissions.UnitsManage)
            .WithName("CreateUnit")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(CatalogPermissions.UnitsManage)
            .WithName("UpdateUnit")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/activate", (Guid id, ICommandHandler<SetUnitActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetUnitActiveCommand(id, IsActive: true), ct)))
            .RequireAuthorization(CatalogPermissions.UnitsManage)
            .WithName("ActivateUnit")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/deactivate", (Guid id, ICommandHandler<SetUnitActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetUnitActiveCommand(id, IsActive: false), ct)))
            .RequireAuthorization(CatalogPermissions.UnitsManage)
            .WithName("DeactivateUnit")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListUnitsQuery, PagedResult<UnitSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, string? search = null, bool? isActive = null) =>
        (await handler.HandleAsync(new ListUnitsQuery(page, pageSize, search, isActive), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> GetAsync(
        Guid id, IQueryHandler<GetUnitQuery, UnitDetailsResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetUnitQuery(id), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> CreateAsync(
        CreateUnitCommand command, ICommandHandler<CreateUnitCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct))
            .ToHttpResult(id => TypedResults.Created($"{Route}/{id}", new CreatedResponse(id)));

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateUnitRequest request, ICommandHandler<UpdateUnitCommand, string> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new UpdateUnitCommand(id, request.NameAr, request.NameEn, request.AllowsFraction, request.RowVersion), ct))
            .ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));

    private static async Task<IResult> NoContentAsync(Task<Result<Done>> pending) =>
        (await pending).ToHttpResult(_ => TypedResults.NoContent());
}
