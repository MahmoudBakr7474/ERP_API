using WarehouseERP.BuildingBlocks.Application.Security;

namespace WarehouseERP.Inventory.Application.Shared;

// Lists show only the warehouses a user may act on; company-wide users see everything.
internal static class WarehouseScope
{
    /// <summary>The user's warehouses for a list filter, or null for a company-wide user.</summary>
    public static IReadOnlyCollection<Guid>? Of(ICurrentUser user) => user.HasAllWarehouses ? null : user.WarehouseIds;
}
