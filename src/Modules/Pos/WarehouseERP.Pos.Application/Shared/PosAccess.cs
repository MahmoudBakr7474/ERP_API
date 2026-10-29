using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Shared;

// Terminals, shifts, sales and returns follow the warehouse scope of their terminal. Scope comes before existence, so a
// scoped user can't tell a missing record from one in a warehouse they don't have.
internal static class PosAccess
{
    /// <summary>Success when the user may act on the record whose warehouse this is (null: no such record).</summary>
    public static Result Check(ICurrentUser user, Guid? warehouseId, Error notFound)
    {
        if (warehouseId is not { } id)
            return user.HasAllWarehouses ? notFound : AccessErrors.WarehouseDenied;
        return user.CanAccessWarehouse(id) ? Result.Success() : AccessErrors.WarehouseDenied;
    }

    /// <summary>The user's warehouses for a list filter, or null for a company-wide user.</summary>
    public static IReadOnlyCollection<Guid>? ScopeOf(ICurrentUser user) => user.HasAllWarehouses ? null : user.WarehouseIds;
}
