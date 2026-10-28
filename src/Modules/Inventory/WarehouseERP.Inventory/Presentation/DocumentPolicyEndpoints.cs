using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Inventory.Application.Contracts.DocumentPolicies;
using WarehouseERP.Inventory.Application.Features.DocumentPolicies.ListDocumentPolicies;
using WarehouseERP.Inventory.Application.Features.DocumentPolicies.UpdateDocumentPolicy;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Presentation.Contracts.DocumentPolicies;

namespace WarehouseERP.Inventory.Presentation;

// Which document types need approval; changing it needs company-wide access as well (checked in the handler).
internal static class DocumentPolicyEndpoints
{
    public const string Route = "/api/inventory/document-policies";

    public static IEndpointRouteBuilder MapDocumentPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("DocumentPolicies");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(InventoryPermissions.DocumentsRead)
            .WithName("ListDocumentPolicies")
            .Produces<IReadOnlyList<DocumentPolicyResponse>>();

        group.MapPut("/{documentType}", UpdateAsync)
            .RequireAuthorization(InventoryPermissions.SettingsManage)
            .WithName("UpdateDocumentPolicy")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListDocumentPoliciesQuery, IReadOnlyList<DocumentPolicyResponse>> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new ListDocumentPoliciesQuery(), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> UpdateAsync(
        StockDocumentType documentType, UpdateDocumentPolicyRequest request,
        ICommandHandler<UpdateDocumentPolicyCommand, string> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new UpdateDocumentPolicyCommand(documentType, request.RequiresApproval, request.RowVersion), ct))
            .ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));
}
