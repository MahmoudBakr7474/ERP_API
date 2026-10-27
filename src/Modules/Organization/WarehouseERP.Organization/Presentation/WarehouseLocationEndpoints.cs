using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.CreateWarehouseLocation;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.GetWarehouseLocation;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.ListWarehouseLocations;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.SetWarehouseLocationActive;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.UpdateWarehouseLocation;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Presentation.Contracts.WarehouseLocations;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Presentation;

// Locations live under their warehouse; every call is limited to the caller's warehouse scope (403 otherwise).
internal static class WarehouseLocationEndpoints
{
    public const string Route = "/api/organization/warehouses/{warehouseId:guid}/locations";

    public static IEndpointRouteBuilder MapWarehouseLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("WarehouseLocations");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(OrganizationPermissions.WarehousesRead)
            .WithName("ListWarehouseLocations")
            .Produces<PagedResult<WarehouseLocationResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{locationId:guid}", GetAsync)
            .RequireAuthorization(OrganizationPermissions.WarehousesRead)
            .WithName("GetWarehouseLocation")
            .Produces<WarehouseLocationResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(OrganizationPermissions.LocationsCreate)
            .WithName("CreateWarehouseLocation")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{locationId:guid}", UpdateAsync)
            .RequireAuthorization(OrganizationPermissions.LocationsUpdate)
            .WithName("UpdateWarehouseLocation")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{locationId:guid}/activate", (Guid warehouseId, Guid locationId,
                    ICommandHandler<SetWarehouseLocationActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetWarehouseLocationActiveCommand(warehouseId, locationId, IsActive: true), ct)))
            .RequireAuthorization(OrganizationPermissions.LocationsUpdate)
            .WithName("ActivateWarehouseLocation")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{locationId:guid}/deactivate", (Guid warehouseId, Guid locationId,
                    ICommandHandler<SetWarehouseLocationActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetWarehouseLocationActiveCommand(warehouseId, locationId, IsActive: false), ct)))
            .RequireAuthorization(OrganizationPermissions.LocationsUpdate)
            .WithName("DeactivateWarehouseLocation")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> ListAsync(
        Guid warehouseId, IQueryHandler<ListWarehouseLocationsQuery, PagedResult<WarehouseLocationResponse>> handler,
        CancellationToken ct, int page = 1, int? pageSize = null, string? search = null, bool? isActive = null) =>
        (await handler.HandleAsync(new ListWarehouseLocationsQuery(warehouseId, page, pageSize, search, isActive), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> GetAsync(
        Guid warehouseId, Guid locationId, IQueryHandler<GetWarehouseLocationQuery, WarehouseLocationResponse> handler,
        CancellationToken ct) =>
        (await handler.HandleAsync(new GetWarehouseLocationQuery(warehouseId, locationId), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> CreateAsync(
        Guid warehouseId, CreateWarehouseLocationRequest request,
        ICommandHandler<CreateWarehouseLocationCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new CreateWarehouseLocationCommand(warehouseId, request.Code, request.NameAr, request.NameEn), ct))
            .ToHttpResult(id => TypedResults.Created(
                $"{WarehouseEndpoints.Route}/{warehouseId}/locations/{id}", new CreatedResponse(id)));

    private static async Task<IResult> UpdateAsync(
        Guid warehouseId, Guid locationId, UpdateWarehouseLocationRequest request,
        ICommandHandler<UpdateWarehouseLocationCommand, string> handler, CancellationToken ct) =>
        (await handler.HandleAsync(
                new UpdateWarehouseLocationCommand(warehouseId, locationId, request.NameAr, request.NameEn, request.RowVersion), ct))
            .ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));

    private static async Task<IResult> NoContentAsync(Task<Result<Done>> pending) =>
        (await pending).ToHttpResult(_ => TypedResults.NoContent());
}
