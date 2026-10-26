namespace WarehouseERP.Identity.Infrastructure.Security;

/// <param name="AllPermissions">The Administrator role: every permission, present and future.</param>
/// <param name="WarehouseIds">Effective warehouses; ignored when <paramref name="AllWarehouses"/> is true.</param>
internal sealed record EffectiveAccess(
    bool AllPermissions, IReadOnlyCollection<string> Permissions, bool AllWarehouses, IReadOnlyCollection<Guid> WarehouseIds);
