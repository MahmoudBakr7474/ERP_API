using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Warehouses.GetWarehouse;

// Scope is checked before existence, so a user outside a warehouse's scope can't learn whether it exists.
public sealed class GetWarehouseHandler(IWarehouseReadStore warehouses, ICurrentUser currentUser)
    : IQueryHandler<GetWarehouseQuery, WarehouseDetails>
{
    public async Task<Result<WarehouseDetails>> HandleAsync(GetWarehouseQuery query, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(query.WarehouseId))
            return OrganizationErrors.WarehouseAccessDenied;

        return await warehouses.GetAsync(query.WarehouseId, ct) is { } warehouse
            ? warehouse
            : OrganizationErrors.WarehouseNotFound;
    }
}
