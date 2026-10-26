using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Features.Authentication.Login;

namespace WarehouseERP.Identity.Presentation;

// The browser's side of a web session: the HttpOnly session cookie, and the anti-forgery pair. ASP.NET Core
// anti-forgery keeps its secret in its own HttpOnly cookie; the matching request token goes in XSRF-TOKEN, which
// the React client reads and sends back as X-XSRF-TOKEN on every unsafe request.
internal static class SessionCookies
{
    public static void SignIn(HttpContext http, IAntiforgery antiforgery, LoginResponse signIn)
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
