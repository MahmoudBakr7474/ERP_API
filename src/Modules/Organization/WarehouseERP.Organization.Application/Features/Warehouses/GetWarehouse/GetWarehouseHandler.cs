using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Warehouses.GetWarehouse;

// Scope is checked before existence, so a user outside a warehouse's scope can't learn whether it exists.
public sealed class GetWarehouseHandler(IWarehouseReadStore warehouses, ICurrentUser currentUser)
    : IQueryHandler<GetWarehouseQuery, WarehouseDetailsResponse>
{
    public async Task<Result<WarehouseDetailsResponse>> HandleAsync(GetWarehouseQuery query, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(query.WarehouseId))
            return WarehouseErrors.AccessDenied;

        return await warehouses.GetAsync(query.WarehouseId, ct) is { } warehouse
            ? warehouse
            : WarehouseErrors.NotFound;
    }
}
