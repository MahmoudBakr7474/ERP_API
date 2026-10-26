using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Presentation;

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
