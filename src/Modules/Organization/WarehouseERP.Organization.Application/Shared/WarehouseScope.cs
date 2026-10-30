using WarehouseERP.BuildingBlocks.Application.Security;

namespace WarehouseERP.Organization.Application.Shared;

internal static class WarehouseScope
{
    /// <summary>The caller's warehouses for a read store filter: null when they may see every warehouse.</summary>
    public static IReadOnlySet<Guid>? Of(ICurrentUser user) => user.HasAllWarehouses ? null : user.WarehouseIds;
}
