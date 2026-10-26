using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Identity.Application.Contracts.Authentication;
using WarehouseERP.Identity.Application.Features.Account.ChangeMyPassword;
using WarehouseERP.Identity.Application.Features.Account.GetMe;
using WarehouseERP.Identity.Application.Features.Authentication.IssueApiToken;
using WarehouseERP.Identity.Application.Features.Authentication.Login;
using WarehouseERP.Identity.Application.Features.Authentication.Logout;
using WarehouseERP.Identity.Application.Features.Authentication.RefreshApiToken;
using WarehouseERP.Identity.Presentation.Contracts.Account;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Presentation;

// Signing in and out. The browser gets an HttpOnly cookie (POST /login); the POS and API tools get a bearer access
// token and a refresh token (POST /tokens). Both are server-side sessions, revocable at once.
internal static class AuthEndpoints
{
    public const string Route = "/api/identity";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Identity");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithName("Login")
            .RequireHttps()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/logout", SignOutAsync)
            .WithName("Logout")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/tokens", IssueTokenAsync)
            .AllowAnonymous()
            .WithName("IssueApiToken")
            .RequireHttps()
            .Produces<ApiTokensResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/tokens/refresh", RefreshTokenAsync)
            .AllowAnonymous()
            .WithName("RefreshApiToken")
            .RequireHttps()
            .Produces<ApiTokensResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/tokens/revoke", SignOutAsync)
            .WithName("RevokeApiToken")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/me", GetMeAsync)
            .WithName("GetMe")
            .Produces<MeResponse>();

        group.MapPost("/me/password", ChangePasswordAsync)
            .WithName("ChangeMyPassword")
            .RequireHttps()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginCommand command, ICommandHandler<LoginCommand, LoginResponse> handler, IAntiforgery antiforgery, HttpContext http,
        CancellationToken ct) =>
        (await handler.HandleAsync(command, ct))
            .ToHttpResult(signIn =>
            {
                SessionCookies.SignIn(http, antiforgery, signIn);
                return TypedResults.NoContent();
            });

    // Ends the caller's own session: the cookie one (and clears the cookies) or the bearer one.
    private static async Task<IResult> SignOutAsync(ICommandHandler<LogoutCommand, Done> handler, HttpContext http, CancellationToken ct)
    {
        var cookieSession = SessionCookies.IsCookieSession(http);
        return (await handler.HandleAsync(new LogoutCommand(SessionId(http)), ct))
            .ToHttpResult(_ =>
            {
                if (cookieSession)
                    SessionCookies.SignOut(http);
                return TypedResults.NoContent();
            });
    }

    private static async Task<IResult> IssueTokenAsync(
        IssueApiTokenCommand command, ICommandHandler<IssueApiTokenCommand, ApiTokensResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct))
            .ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> RefreshTokenAsync(
        RefreshApiTokenCommand command, ICommandHandler<RefreshApiTokenCommand, ApiTokensResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct)).ToHttpResult(TypedResults.Ok);

    // Also renews the anti-forgery token of a cookie session: the React client calls this after signing in or reloading.
    private static async Task<IResult> GetMeAsync(
        IQueryHandler<GetMeQuery, MeResponse> handler, IAntiforgery antiforgery, HttpContext http, CancellationToken ct) =>
        (await handler.HandleAsync(new GetMeQuery(), ct))
            .ToHttpResult(me =>
            {
                if (SessionCookies.IsCookieSession(http))
                    SessionCookies.IssueXsrfToken(http, antiforgery);
                return TypedResults.Ok(me);
            });

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request, ICommandHandler<ChangeMyPasswordCommand, Done> handler, HttpContext http, CancellationToken ct) =>
        (await handler.HandleAsync(new ChangeMyPasswordCommand(request.CurrentPassword, request.NewPassword, SessionId(http)), ct))
            .ToHttpResult(_ => TypedResults.NoContent());

    private static Guid SessionId(HttpContext http) =>
        Guid.TryParse(http.User.FindFirstValue(ErpClaimTypes.SessionId), out var id) ? id : Guid.Empty;
}
