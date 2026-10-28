using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.GetPurchaseRequest;

public sealed class GetPurchaseRequestHandler(IPurchaseRequestRepository requests, IPurchaseRequestReadStore readStore, ICurrentUser currentUser)
    : IQueryHandler<GetPurchaseRequestQuery, PurchaseRequestDetailsResponse>
{
    public async Task<Result<PurchaseRequestDetailsResponse>> HandleAsync(GetPurchaseRequestQuery query, CancellationToken ct)
    {
        var access = await RequestAccess.AuthorizeAsync(requests, currentUser, query.PurchaseRequestId, ct);
        if (access.IsFailure)
            return access.Error;
        return await readStore.GetAsync(query.PurchaseRequestId, ct) is { } request ? request : PurchaseRequestErrors.NotFound;
    }
}
