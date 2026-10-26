using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Identity.Application.Features.Login;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Presentation;

// The browser's side of a web session: the HttpOnly session cookie, and the anti-forgery pair. ASP.NET Core
// anti-forgery keeps its secret in its own HttpOnly cookie; the matching request token goes in XSRF-TOKEN, which
// the React client reads and sends back as X-XSRF-TOKEN on every unsafe request.
internal static class SessionCookies
{
    public static void SignIn(HttpContext http, IAntiforgery antiforgery, WebSignIn signIn)
    {
        http.Response.Cookies.Append(IdentityHttpDefaults.SessionCookie, signIn.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
            Expires = new DateTimeOffset(DateTime.SpecifyKind(signIn.ExpiresAtUtc, DateTimeKind.Utc)),
        });
        // Anti-forgery tokens are bound to the user ("sub"), so issue them for the user who just signed in.
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ErpClaimTypes.Subject, signIn.UserId.ToString())], IdentityHttpDefaults.CookieAuthenticationType));
        IssueXsrfToken(http, antiforgery);
    }

    public static void IssueXsrfToken(HttpContext http, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(http);
        http.Response.Cookies.Append(IdentityHttpDefaults.XsrfCookie, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false, // read by the client, by design
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
        });
    }

    public static void SignOut(HttpContext http)
    {
        var options = new CookieOptions { Secure = true, SameSite = SameSiteMode.Strict, Path = "/" };
        http.Response.Cookies.Delete(IdentityHttpDefaults.SessionCookie, options);
        http.Response.Cookies.Delete(IdentityHttpDefaults.XsrfCookie, options);
    }

    public static bool IsCookieSession(HttpContext http) =>
        http.User.Identity?.AuthenticationType == IdentityHttpDefaults.CookieAuthenticationType;
}

// Passwords and session tokens never travel over plain HTTP; the Secure cookies and anti-forgery need HTTPS anyway.
// Behind a TLS-terminating proxy the host must apply forwarded headers, or every sign-in is refused with this code.
internal static class HttpsOnly
{
    public static readonly Error Required = Error.Forbidden(
        "Identity.Session.HttpsRequired", "Signing in and handling credentials require HTTPS.");

    public static RouteHandlerBuilder RequireHttps(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter(async (context, next) =>
            context.HttpContext.Request.IsHttps ? await next(context) : Required.ToProblem());
}

// Cookies are sent by the browser on its own, so a cookie-authenticated unsafe request must also prove it came from
// our client (CSRF), on top of SameSite=Strict. Bearer requests carry their token explicitly and need no check.
internal sealed class CookieSessionAntiforgeryMiddleware(RequestDelegate next, IAntiforgery antiforgery)
{
    public static readonly Error CsrfTokenInvalid = Error.Forbidden(
        "Identity.Session.CsrfTokenInvalid", "The anti-forgery token is missing or invalid; reload the page and try again.");

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var safe = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);
        if (!safe && SessionCookies.IsCookieSession(context) && !await antiforgery.IsRequestValidAsync(context))
        {
            await CsrfTokenInvalid.ToProblem().ExecuteAsync(context);
            return;
        }
        await next(context);
    }
}
