using WarehouseERP.BuildingBlocks.Application.Security;

namespace WarehouseERP.Identity.Application.Features.Roles;

internal static class PermissionCatalog
{
    /// <summary>Every permission name registered by the modules.</summary>
    public static IReadOnlySet<string> Names(IEnumerable<PermissionDefinition> catalog) =>
        catalog.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
}
