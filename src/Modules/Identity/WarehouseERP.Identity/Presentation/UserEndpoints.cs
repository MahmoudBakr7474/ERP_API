using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Application.Features.Users.CreateUser;
using WarehouseERP.Identity.Application.Features.Users.GetUser;
using WarehouseERP.Identity.Application.Features.Sessions.ListSessions;
using WarehouseERP.Identity.Application.Features.Users.ListUsers;
using WarehouseERP.Identity.Application.Features.Users.ResetUserPassword;
using WarehouseERP.Identity.Application.Features.Sessions.RevokeUserSessions;
using WarehouseERP.Identity.Application.Features.Users.SetUserAccess;
using WarehouseERP.Identity.Application.Features.Users.SetUserActive;
using WarehouseERP.Identity.Application.Features.Users.SetUserRoles;
using WarehouseERP.Identity.Application.Features.Users.UnlockUser;
using WarehouseERP.Identity.Application.Features.Users.UpdateUser;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Presentation;

// User administration. Lists in requests are optional in JSON; a missing one means empty.
internal static class UserEndpoints
{
    public const string Route = "/api/identity/users";

    public sealed record CreateUserRequest(
        string UserName, string DisplayName, string? Email, string Password, IReadOnlyList<Guid>? RoleIds,
        bool IsCompanyWide, IReadOnlyList<Guid>? BranchIds, IReadOnlyList<Guid>? WarehouseIds);

    public sealed record UpdateUserRequest(string DisplayName, string? Email);

    public sealed record ResetPasswordRequest(string NewPassword);

    public sealed record SetRolesRequest(IReadOnlyList<Guid>? RoleIds);

    public sealed record SetAccessRequest(bool IsCompanyWide, IReadOnlyList<Guid>? BranchIds, IReadOnlyList<Guid>? WarehouseIds);

    public sealed record CreatedResponse(Guid Id);

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Users");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(IdentityPermissions.UsersRead)
            .WithName("ListUsers")
            .Produces<PagedResult<UserSummary>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(IdentityPermissions.UsersRead)
            .WithName("GetUser")
            .Produces<UserDetails>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(IdentityPermissions.UsersManage)
            .WithName("CreateUser")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(IdentityPermissions.UsersManage)
            .WithName("UpdateUser")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/activate", (Guid id, ICommandHandler<SetUserActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetUserActiveCommand(id, IsActive: true), ct)))
            .RequireAuthorization(IdentityPermissions.UsersManage)
            .WithName("ActivateUser")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/{id:guid}/deactivate", (Guid id, ICommandHandler<SetUserActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetUserActiveCommand(id, IsActive: false), ct)))
            .RequireAuthorization(IdentityPermissions.UsersManage)
            .WithName("DeactivateUser")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/unlock", (Guid id, ICommandHandler<UnlockUserCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new UnlockUserCommand(id), ct)))
            .RequireAuthorization(IdentityPermissions.UsersManage)
            .WithName("UnlockUser")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/{id:guid}/password", (Guid id, ResetPasswordRequest request, ICommandHandler<ResetUserPasswordCommand, Done> handler,
                    CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new ResetUserPasswordCommand(id, request.NewPassword), ct)))
            .RequireAuthorization(IdentityPermissions.UsersManage)
            .WithName("ResetUserPassword")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}/roles", (Guid id, SetRolesRequest request, ICommandHandler<SetUserRolesCommand, Done> handler,
                    CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetUserRolesCommand(id, request.RoleIds ?? []), ct)))
            .RequireAuthorization(IdentityPermissions.UsersManage)
            .WithName("SetUserRoles")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:guid}/access", (Guid id, SetAccessRequest request, ICommandHandler<SetUserAccessCommand, Done> handler,
                    CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(
                    new SetUserAccessCommand(id, request.IsCompanyWide, request.BranchIds ?? [], request.WarehouseIds ?? []), ct)))
            .RequireAuthorization(IdentityPermissions.AccessManage)
            .WithName("SetUserAccess")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}/sessions", async (Guid id, IQueryHandler<ListSessionsQuery, IReadOnlyList<SessionItem>> handler,
                    CancellationToken ct) =>
                (await handler.HandleAsync(new ListSessionsQuery(id), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(IdentityPermissions.SessionsManage)
            .WithName("ListUserSessions")
            .Produces<IReadOnlyList<SessionItem>>();

        group.MapPost("/{id:guid}/sessions/revoke", (Guid id, ICommandHandler<RevokeUserSessionsCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new RevokeUserSessionsCommand(id), ct)))
            .RequireAuthorization(IdentityPermissions.SessionsManage)
            .WithName("RevokeUserSessions")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListUsersQuery, PagedResult<UserSummary>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, string? search = null) =>
        (await handler.HandleAsync(new ListUsersQuery(page, pageSize, search), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> GetAsync(Guid id, IQueryHandler<GetUserQuery, UserDetails> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetUserQuery(id), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> CreateAsync(
        CreateUserRequest request, ICommandHandler<CreateUserCommand, Guid> handler, CancellationToken ct)
    {
        var command = new CreateUserCommand(
            request.UserName, request.DisplayName, request.Email, request.Password, request.RoleIds ?? [],
            request.IsCompanyWide, request.BranchIds ?? [], request.WarehouseIds ?? []);
        return (await handler.HandleAsync(command, ct))
            .ToHttpResult(id => TypedResults.Created($"{Route}/{id}", new CreatedResponse(id)));
    }

    private static Task<IResult> UpdateAsync(
        Guid id, UpdateUserRequest request, ICommandHandler<UpdateUserCommand, Done> handler, CancellationToken ct) =>
        NoContentAsync(handler.HandleAsync(new UpdateUserCommand(id, request.DisplayName, request.Email), ct));

    internal static async Task<IResult> NoContentAsync(Task<Result<Done>> pending) =>
        (await pending).ToHttpResult(_ => TypedResults.NoContent());
}
