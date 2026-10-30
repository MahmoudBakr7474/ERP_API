using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Application.Features.Warehouses.CreateWarehouse;
using WarehouseERP.Organization.Application.Features.Warehouses.GetWarehouse;
using WarehouseERP.Organization.Application.Features.Warehouses.ListWarehouses;
using WarehouseERP.Organization.Application.Features.Warehouses.SetWarehouseActive;
using WarehouseERP.Organization.Application.Features.Warehouses.UpdateWarehouse;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Presentation.Contracts.Warehouses;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Presentation;

// HTTP only: bind the request, map it field by field to a command or query (never bind an entity),
// call the handler pipeline and map the Result. No EF Core here.
internal static class WarehouseEndpoints
{
    public const string Route = "/api/organization/warehouses";

    public static IEndpointRouteBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Warehouses");

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(OrganizationPermissions.WarehousesCreate)
            .WithName("CreateWarehouse")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(OrganizationPermissions.WarehousesRead)
            .WithName("GetWarehouse")
            .Produces<WarehouseDetailsResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", ListAsync)
            .RequireAuthorization(OrganizationPermissions.WarehousesRead)
            .WithName("ListWarehouses")
            .Produces<PagedResult<WarehouseSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(OrganizationPermissions.WarehousesUpdate)
            .WithName("UpdateWarehouse")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/activate", (Guid id, ICommandHandler<SetWarehouseActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetWarehouseActiveCommand(id, IsActive: true), ct)))
            .RequireAuthorization(OrganizationPermissions.WarehousesUpdate)
            .WithName("ActivateWarehouse")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/deactivate", (Guid id, ICommandHandler<SetWarehouseActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetWarehouseActiveCommand(id, IsActive: false), ct)))
            .RequireAuthorization(OrganizationPermissions.WarehousesUpdate)
            .WithName("DeactivateWarehouse")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateWarehouseCommand command, ICommandHandler<CreateWarehouseCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct))
            .ToHttpResult(id => TypedResults.Created($"{Route}/{id}", new CreatedResponse(id)));

    private static async Task<IResult> GetAsync(
        Guid id, IQueryHandler<GetWarehouseQuery, WarehouseDetailsResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetWarehouseQuery(id), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListWarehousesQuery, PagedResult<WarehouseSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? branchId = null, string? search = null, bool? isActive = null,
        WarehouseType? type = null, bool? allowsSelling = null, OrganizationSort? sort = null, bool descending = false) =>
        (await handler.HandleAsync(
                new ListWarehousesQuery(page, pageSize, branchId, search, isActive, type, allowsSelling, sort, descending), ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateWarehouseRequest request, ICommandHandler<UpdateWarehouseCommand, string> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new UpdateWarehouseCommand(id, request.NameAr, request.NameEn, request.AllowsSelling, request.RowVersion), ct))
            .ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));

    private static async Task<IResult> NoContentAsync(Task<Result<Done>> pending) =>
        (await pending).ToHttpResult(_ => TypedResults.NoContent());
}
