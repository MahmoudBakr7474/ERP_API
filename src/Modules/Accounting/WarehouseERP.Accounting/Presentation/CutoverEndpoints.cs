using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.Accounting.Application.Contracts.Cutover;
using WarehouseERP.Accounting.Application.Features.Cutover.ActivateAccounting;
using WarehouseERP.Accounting.Application.Features.Cutover.GetCutoverReadiness;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;

namespace WarehouseERP.Accounting.Presentation;

// The cutover: its checks, and the explicit activation of automatic accounting once they pass.
internal static class CutoverEndpoints
{
    public const string Route = "/api/accounting/cutover";

    public static IEndpointRouteBuilder MapCutoverEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Cutover");

        group.MapGet("/", (IQueryHandler<GetCutoverReadinessQuery, CutoverReadinessResponse> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new GetCutoverReadinessQuery(), ct)))
            .RequireAuthorization(AccountingPermissions.ReportsRead)
            .WithName("GetCutoverReadiness")
            .Produces<CutoverReadinessResponse>()
            .QueryProblems();

        group.MapPost("/activate", (ActivateAccountingCommand command, ICommandHandler<ActivateAccountingCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(command, ct)))
            .RequireAuthorization(AccountingPermissions.IntegrationManage)
            .WithName("ActivateAccounting")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }
}
