using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Application.Features.Roles.CreateRole;
using WarehouseERP.Identity.Application.Features.Roles.DeleteRole;
using WarehouseERP.Identity.Application.Features.Roles.GetRole;
using WarehouseERP.Identity.Application.Features.Permissions.ListPermissions;
using WarehouseERP.Identity.Application.Features.Roles.ListRoles;
using WarehouseERP.Identity.Application.Features.Sessions.ListSessions;
using WarehouseERP.Identity.Application.Features.Sessions.RevokeSession;
using WarehouseERP.Identity.Application.Features.Roles.UpdateRole;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Presentation;

// Roles and the permission catalog they draw from, and the caller's own sessions.
internal static class RoleEndpoints
{
    public const string Route = "/api/identity/roles";

    public sealed record RoleRequest(string Name, string? Description, IReadOnlyList<string>? Permissions);

    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var roles = app.MapGroup(Route).WithTags("Roles");

        roles.MapGet("/", async (IQueryHandler<ListRolesQuery, IReadOnlyList<RoleSummary>> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new ListRolesQuery(), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(IdentityPermissions.RolesRead)
            .WithName("ListRoles")
            .Produces<IReadOnlyList<RoleSummary>>();

        roles.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetRoleQuery, RoleDetails> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetRoleQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(IdentityPermissions.RolesRead)
            .WithName("GetRole")
            .Produces<RoleDetails>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        roles.MapPost("/", async (RoleRequest request, ICommandHandler<CreateRoleCommand, Guid> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new CreateRoleCommand(request.Name, request.Description, request.Permissions ?? []), ct))
                .ToHttpResult(id => TypedResults.Created($"{Route}/{id}", new UserEndpoints.CreatedResponse(id))))
            .RequireAuthorization(IdentityPermissions.RolesManage)
            .WithName("CreateRole")
            .Produces<UserEndpoints.CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        roles.MapPut("/{id:guid}", (Guid id, RoleRequest request, ICommandHandler<UpdateRoleCommand, Done> handler, CancellationToken ct) =>
                UserEndpoints.NoContentAsync(handler.HandleAsync(
                    new UpdateRoleCommand(id, request.Name, request.Description, request.Permissions ?? []), ct)))
            .RequireAuthorization(IdentityPermissions.RolesManage)
            .WithName("UpdateRole")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        roles.MapDelete("/{id:guid}", (Guid id, ICommandHandler<DeleteRoleCommand, Done> handler, CancellationToken ct) =>
                UserEndpoints.NoContentAsync(handler.HandleAsync(new DeleteRoleCommand(id), ct)))
            .RequireAuthorization(IdentityPermissions.RolesManage)
            .WithName("DeleteRole")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        app.MapGet("/api/identity/permissions", async (IQueryHandler<ListPermissionsQuery, IReadOnlyList<PermissionDefinition>> handler,
                    CancellationToken ct) =>
                (await handler.HandleAsync(new ListPermissionsQuery(), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(IdentityPermissions.RolesRead)
            .WithTags("Roles")
            .WithName("ListPermissions")
            .Produces<IReadOnlyList<PermissionDefinition>>();

        var sessions = app.MapGroup("/api/identity/sessions").WithTags("Sessions");

        sessions.MapGet("/", async (IQueryHandler<ListSessionsQuery, IReadOnlyList<SessionItem>> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new ListSessionsQuery(UserId: null), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("ListMySessions")
            .Produces<IReadOnlyList<SessionItem>>();

        sessions.MapPost("/{id:guid}/revoke", (Guid id, ICommandHandler<RevokeSessionCommand, Done> handler, CancellationToken ct) =>
                UserEndpoints.NoContentAsync(handler.HandleAsync(new RevokeSessionCommand(id), ct)))
            .WithName("RevokeSession")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
