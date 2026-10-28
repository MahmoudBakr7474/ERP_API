using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Purchasing.Application.Contracts.PurchasingPolicies;
using WarehouseERP.Purchasing.Application.Features.PurchasingPolicies.ListPurchasingPolicies;
using WarehouseERP.Purchasing.Application.Features.PurchasingPolicies.UpdatePurchasingPolicy;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Presentation.Contracts.PurchasingPolicies;

namespace WarehouseERP.Purchasing.Presentation;

// Whether purchase requests and orders need approval. Changing it needs purchasing.settings.manage and company-wide access.
internal static class PurchasingPolicyEndpoints
{
    public const string Route = "/api/purchasing/policies";

    public static IEndpointRouteBuilder MapPurchasingPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("PurchasingPolicies");

        group.MapGet("/", async (IQueryHandler<ListPurchasingPoliciesQuery, IReadOnlyList<PurchasingPolicyResponse>> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new ListPurchasingPoliciesQuery(), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(PurchasingPermissions.DocumentsRead)
            .WithName("ListPurchasingPolicies")
            .Produces<IReadOnlyList<PurchasingPolicyResponse>>();

        group.MapPut("/{documentType}", (PurchasingDocumentType documentType, UpdatePurchasingPolicyRequest request,
                    ICommandHandler<UpdatePurchasingPolicyCommand, string> handler, CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(
                    new UpdatePurchasingPolicyCommand(documentType, request.RequiresApproval, request.RowVersion), ct)))
            .RequireAuthorization(PurchasingPermissions.SettingsManage)
            .WithName("UpdatePurchasingPolicy")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }
}
