using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Sales.Application.Features.Settings.GetSalesSettings;
using WarehouseERP.Sales.Application.Features.Settings.UpdateSalesSettings;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Presentation;

// Reading the sales settings needs sales.documents.read; changing them sales.settings.manage and company-wide access
// (checked by the handler).
internal static class SalesSettingsEndpoints
{
    public const string Route = "/api/sales/settings";

    public static IEndpointRouteBuilder MapSalesSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("SalesSettings");

        group.MapGet("/", async (IQueryHandler<GetSalesSettingsQuery, SalesSettingsResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetSalesSettingsQuery(), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("GetSalesSettings")
            .Produces<SalesSettingsResponse>();

        group.MapPut("/", (UpdateSalesSettingsCommand command, ICommandHandler<UpdateSalesSettingsCommand, string> handler, CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(command, ct)))
            .RequireAuthorization(SalesPermissions.SettingsManage)
            .WithName("UpdateSalesSettings")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }
}
