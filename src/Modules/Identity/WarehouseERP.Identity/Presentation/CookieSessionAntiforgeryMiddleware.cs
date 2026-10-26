using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Presentation;

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
