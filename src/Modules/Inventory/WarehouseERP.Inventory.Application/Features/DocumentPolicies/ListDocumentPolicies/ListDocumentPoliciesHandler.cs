using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Inventory.Application.Contracts.DocumentPolicies;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.DocumentPolicies.ListDocumentPolicies;

public sealed class ListDocumentPoliciesHandler(IDocumentPolicyRepository policies)
    : IQueryHandler<ListDocumentPoliciesQuery, IReadOnlyList<DocumentPolicyResponse>>
{
    public async Task<Result<IReadOnlyList<DocumentPolicyResponse>>> HandleAsync(ListDocumentPoliciesQuery query, CancellationToken ct) =>
        Result.Success(await policies.ListAsync(ct));
}
