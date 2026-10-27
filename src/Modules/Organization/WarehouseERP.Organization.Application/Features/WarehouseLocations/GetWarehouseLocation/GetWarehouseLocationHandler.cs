using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.GetWarehouseLocation;

public sealed class GetWarehouseLocationHandler(IWarehouseReadStore warehouses, ICurrentUser currentUser)
    : IQueryHandler<GetWarehouseLocationQuery, WarehouseLocationResponse>
{
    public async Task<Result<WarehouseLocationResponse>> HandleAsync(GetWarehouseLocationQuery query, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(query.WarehouseId))
            return WarehouseErrors.AccessDenied;

        return await warehouses.GetLocationAsync(query.WarehouseId, query.LocationId, ct) is { } location
            ? location
            : WarehouseLocationErrors.NotFound;
    }
}
