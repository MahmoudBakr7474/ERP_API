using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Application.Contracts.Deliveries;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Deliveries.GetSalesDelivery;

public sealed class GetSalesDeliveryHandler(
    ISalesDeliveryRepository deliveries, ISalesDeliveryReadStore readStore, ICurrentUser currentUser)
    : IQueryHandler<GetSalesDeliveryQuery, SalesDeliveryDetailsResponse>
{
    public async Task<Result<SalesDeliveryDetailsResponse>> HandleAsync(GetSalesDeliveryQuery query, CancellationToken ct)
    {
        var access = await DeliveryAccess.AuthorizeAsync(deliveries, currentUser, query.DeliveryId, ct);
        if (access.IsFailure)
            return access.Error;
        return await readStore.GetAsync(query.DeliveryId, ct) is { } delivery ? delivery : DeliveryErrors.NotFound;
    }
}
