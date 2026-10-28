using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.GetPurchaseOrder;

public sealed class GetPurchaseOrderHandler(IPurchaseOrderRepository orders, IPurchaseOrderReadStore readStore, ICurrentUser currentUser)
    : IQueryHandler<GetPurchaseOrderQuery, PurchaseOrderDetailsResponse>
{
    public async Task<Result<PurchaseOrderDetailsResponse>> HandleAsync(GetPurchaseOrderQuery query, CancellationToken ct)
    {
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, query.PurchaseOrderId, ct);
        if (access.IsFailure)
            return access.Error;
        return await readStore.GetAsync(query.PurchaseOrderId, ct) is { } order ? order : PurchaseOrderErrors.NotFound;
    }
}
