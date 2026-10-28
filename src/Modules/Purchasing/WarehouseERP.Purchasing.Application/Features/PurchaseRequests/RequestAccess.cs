using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests;

// A request belongs to its warehouse's scope. The endpoint already checked the permission of the action.
internal static class RequestAccess
{
    public static async Task<Result> AuthorizeAsync(IPurchaseRequestRepository requests, ICurrentUser user, Guid requestId, CancellationToken ct) =>
        DocumentAccess.Check(user, await requests.FindWarehouseAsync(requestId, ct), PurchaseRequestErrors.NotFound);
}
