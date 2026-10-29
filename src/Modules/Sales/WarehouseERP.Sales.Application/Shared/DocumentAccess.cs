using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Shared;

// Sales documents follow the warehouse scope of their warehouse. Scope comes before existence, so a scoped user can't tell
// a missing document from one in a warehouse they don't have.
internal static class DocumentAccess
{
    /// <summary>Success when the user may see the document whose warehouse this is (null: no such document).</summary>
    public static Result Check(ICurrentUser user, Guid? warehouseId, Error notFound)
    {
        if (warehouseId is not { } id)
            return user.HasAllWarehouses ? notFound : AccessErrors.WarehouseDenied;
        return user.CanAccessWarehouse(id) ? Result.Success() : AccessErrors.WarehouseDenied;
    }

    /// <summary>The user's warehouses for a list filter, or null for a company-wide user.</summary>
    public static IReadOnlyCollection<Guid>? ScopeOf(ICurrentUser user) => user.HasAllWarehouses ? null : user.WarehouseIds;
}
