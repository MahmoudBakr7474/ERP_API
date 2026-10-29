using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders;

// An order belongs to its warehouse's scope. The endpoint already checked the permission of the action.
internal static class OrderAccess
{
    public static async Task<Result> AuthorizeAsync(ISalesOrderRepository orders, ICurrentUser user, Guid orderId, CancellationToken ct) =>
        DocumentAccess.Check(user, await orders.FindWarehouseAsync(orderId, ct), OrderErrors.NotFound);
}
