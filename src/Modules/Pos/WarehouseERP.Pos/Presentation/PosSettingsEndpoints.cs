using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Pos.Application.Features.Settings.GetPosSettings;
using WarehouseERP.Pos.Application.Features.Settings.UpdatePosSettings;
using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Presentation;

// Any signed-in user reads the POS policies (a till shows the discount limit); changing them needs pos.settings.manage and
// company-wide access (checked by the handler).
internal static class PosSettingsEndpoints
{
    public const string Route = "/api/pos/settings";

    public static IEndpointRouteBuilder MapPosSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("PosSettings");

        group.MapGet("/", async (IQueryHandler<GetPosSettingsQuery, PosSettingsResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetPosSettingsQuery(), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization()
            .WithName("GetPosSettings")
            .Produces<PosSettingsResponse>();

        group.MapPut("/", (UpdatePosSettingsCommand command, ICommandHandler<UpdatePosSettingsCommand, string> handler, CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(command, ct)))
            .RequireAuthorization(PosPermissions.SettingsManage)
            .WithName("UpdatePosSettings")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }
}
