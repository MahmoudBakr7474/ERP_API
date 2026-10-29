using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Contracts.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders.GetSalesOrder;

public sealed class GetSalesOrderHandler(ISalesOrderRepository orders, ISalesOrderReadStore readStore, ICurrentUser currentUser)
    : IQueryHandler<GetSalesOrderQuery, SalesOrderDetailsResponse>
{
    public async Task<Result<SalesOrderDetailsResponse>> HandleAsync(GetSalesOrderQuery query, CancellationToken ct)
    {
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, query.OrderId, ct);
        if (access.IsFailure)
            return access.Error;
        return await readStore.GetAsync(query.OrderId, ct) is { } order ? order : OrderErrors.NotFound;
    }
}
