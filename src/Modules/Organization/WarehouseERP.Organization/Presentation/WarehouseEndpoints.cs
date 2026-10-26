using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Organization.Application.Abstractions;
using WarehouseERP.Organization.Application.Features.CreateWarehouse;
using WarehouseERP.Organization.Application.Features.GetWarehouse;
using WarehouseERP.Organization.Application.Features.ListWarehouses;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Presentation;

// HTTP only: bind the request, map it field by field to a command or query (never bind an entity),
// call the handler pipeline and map the Result. No EF Core here.
internal static class WarehouseEndpoints
{
    public const string Route = "/api/organization/warehouses";

    public sealed record CreateWarehouseRequest(
        Guid BranchId, string Code, string NameAr, string? NameEn, WarehouseType Type, bool AllowsSelling);

    public sealed record CreatedResponse(Guid Id);

    public static IEndpointRouteBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Warehouses");

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(OrganizationPermissions.WarehousesCreate)
            .WithName("CreateWarehouse")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(OrganizationPermissions.WarehousesRead)
            .WithName("GetWarehouse")
            .Produces<WarehouseDetails>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", ListAsync)
            .RequireAuthorization(OrganizationPermissions.WarehousesRead)
            .WithName("ListWarehouses")
            .Produces<PagedResult<WarehouseSummary>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateWarehouseRequest request, ICommandHandler<CreateWarehouseCommand, Guid> handler, CancellationToken ct)
    {
        var command = new CreateWarehouseCommand(
            request.BranchId, request.Code, request.NameAr, request.NameEn, request.Type, request.AllowsSelling);
        return (await handler.HandleAsync(command, ct))
            .ToHttpResult(id => TypedResults.Created($"{Route}/{id}", new CreatedResponse(id)));
    }

    private static async Task<IResult> GetAsync(
        Guid id, IQueryHandler<GetWarehouseQuery, WarehouseDetails> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetWarehouseQuery(id), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListWarehousesQuery, PagedResult<WarehouseSummary>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? branchId = null) =>
        (await handler.HandleAsync(new ListWarehousesQuery(page, pageSize, branchId), ct)).ToHttpResult(TypedResults.Ok);
}
