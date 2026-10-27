using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Organization.Application.Contracts.Branches;
using WarehouseERP.Organization.Application.Features.Branches.CreateBranch;
using WarehouseERP.Organization.Application.Features.Branches.GetBranch;
using WarehouseERP.Organization.Application.Features.Branches.ListBranches;
using WarehouseERP.Organization.Application.Features.Branches.SetBranchActive;
using WarehouseERP.Organization.Application.Features.Branches.UpdateBranch;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Presentation.Contracts.Branches;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Presentation;

// HTTP only: bind the request, map it to a command or query, call the handler pipeline and map the Result.
// Reads are scoped in the handlers; every change needs company-wide access (403 otherwise).
internal static class BranchEndpoints
{
    public const string Route = "/api/organization/branches";

    public static IEndpointRouteBuilder MapBranchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Branches");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(OrganizationPermissions.BranchesRead)
            .WithName("ListBranches")
            .Produces<PagedResult<BranchSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(OrganizationPermissions.BranchesRead)
            .WithName("GetBranch")
            .Produces<BranchDetailsResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(OrganizationPermissions.BranchesCreate)
            .WithName("CreateBranch")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(OrganizationPermissions.BranchesUpdate)
            .WithName("UpdateBranch")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/activate", (Guid id, ICommandHandler<SetBranchActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetBranchActiveCommand(id, IsActive: true), ct)))
            .RequireAuthorization(OrganizationPermissions.BranchesUpdate)
            .WithName("ActivateBranch")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/deactivate", (Guid id, ICommandHandler<SetBranchActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetBranchActiveCommand(id, IsActive: false), ct)))
            .RequireAuthorization(OrganizationPermissions.BranchesUpdate)
            .WithName("DeactivateBranch")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListBranchesQuery, PagedResult<BranchSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, string? search = null, bool? isActive = null) =>
        (await handler.HandleAsync(new ListBranchesQuery(page, pageSize, search, isActive), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> GetAsync(
        Guid id, IQueryHandler<GetBranchQuery, BranchDetailsResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetBranchQuery(id), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> CreateAsync(
        CreateBranchCommand command, ICommandHandler<CreateBranchCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct))
            .ToHttpResult(id => TypedResults.Created($"{Route}/{id}", new CreatedResponse(id)));

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateBranchRequest request, ICommandHandler<UpdateBranchCommand, string> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new UpdateBranchCommand(id, request.NameAr, request.NameEn, request.RowVersion), ct))
            .ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));

    private static async Task<IResult> NoContentAsync(Task<Result<Done>> pending) =>
        (await pending).ToHttpResult(_ => TypedResults.NoContent());
}
