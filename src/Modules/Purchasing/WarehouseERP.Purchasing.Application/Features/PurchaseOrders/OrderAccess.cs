using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders;

// An order belongs to its warehouse's scope. The endpoint already checked the permission of the action.
internal static class OrderAccess
{
    public static async Task<Result> AuthorizeAsync(IPurchaseOrderRepository orders, ICurrentUser user, Guid orderId, CancellationToken ct) =>
        DocumentAccess.Check(user, await orders.FindWarehouseAsync(orderId, ct), PurchaseOrderErrors.NotFound);
}
