using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Inventory.Application.Contracts.Reconciliation;
using WarehouseERP.Inventory.Application.Features.Reconciliation.GetReconciliation;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Presentation;

// One report: inventory checked against itself; it lists differences and corrects nothing.
internal static class ReconciliationEndpoints
{
    public const string Route = "/api/inventory/reconciliation";

    public static IEndpointRouteBuilder MapReconciliationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Route, GetAsync)
            .WithTags("Reconciliation")
            .RequireAuthorization(InventoryPermissions.ReconciliationRead)
            .WithName("GetInventoryReconciliation")
            .Produces<ReconciliationReport>()
            .ProducesProblem(StatusCodes.Status403Forbidden);
        return app;
    }

    private static async Task<IResult> GetAsync(
        IQueryHandler<GetReconciliationQuery, ReconciliationReport> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetReconciliationQuery(), ct)).ToHttpResult(TypedResults.Ok);
}
