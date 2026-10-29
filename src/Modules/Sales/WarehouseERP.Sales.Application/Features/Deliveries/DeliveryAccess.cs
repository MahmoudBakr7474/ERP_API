using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Deliveries;

// A delivery belongs to its warehouse's scope (its order's). The endpoint already checked the permission of the action.
internal static class DeliveryAccess
{
    public static async Task<Result> AuthorizeAsync(ISalesDeliveryRepository deliveries, ICurrentUser user, Guid deliveryId, CancellationToken ct) =>
        DocumentAccess.Check(user, await deliveries.FindWarehouseAsync(deliveryId, ct), DeliveryErrors.NotFound);
}
