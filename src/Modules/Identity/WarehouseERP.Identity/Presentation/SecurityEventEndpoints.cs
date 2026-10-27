using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Identity.Application.Contracts.SecurityEvents;
using WarehouseERP.Identity.Application.Features.SecurityEvents.ListSecurityEvents;
using WarehouseERP.Identity.Contracts;

namespace WarehouseERP.Identity.Presentation;

// The security audit trail, read-only: sign-ins, password, role, permission and scope changes, revocations and
// refused escalation attempts. The query string binds the query itself.
internal static class SecurityEventEndpoints
{
    public const string Route = "/api/identity/security-events";

    public static IEndpointRouteBuilder MapSecurityEventEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Route, ListAsync)
            .RequireAuthorization(IdentityPermissions.AuditRead)
            .WithTags("Security events")
            .WithName("ListSecurityEvents")
            .Produces<PagedResult<SecurityEventResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
        return app;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] ListSecurityEventsQuery query, IQueryHandler<ListSecurityEventsQuery, PagedResult<SecurityEventResponse>> handler,
        CancellationToken ct) =>
        (await handler.HandleAsync(query, ct)).ToHttpResult(TypedResults.Ok);
}
