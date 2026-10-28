using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Purchasing.Application.Contracts.PurchasingPolicies;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchasingPolicies.ListPurchasingPolicies;

public sealed class ListPurchasingPoliciesHandler(IDocumentPolicyRepository policies)
    : IQueryHandler<ListPurchasingPoliciesQuery, IReadOnlyList<PurchasingPolicyResponse>>
{
    public async Task<Result<IReadOnlyList<PurchasingPolicyResponse>>> HandleAsync(ListPurchasingPoliciesQuery query, CancellationToken ct) =>
        Result.Success(await policies.ListAsync(ct));
}
