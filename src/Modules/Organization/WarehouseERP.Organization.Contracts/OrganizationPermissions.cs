namespace WarehouseERP.Organization.Contracts;

/// <summary>
/// Permissions Organization endpoints require. Public so the host can register policies and a
/// future Identity module can grant them.
/// </summary>
public static class OrganizationPermissions
{
    public const string WarehousesRead = "organization.warehouses.read";
    public const string WarehousesCreate = "organization.warehouses.create";

    public static IReadOnlyList<string> All { get; } = [WarehousesRead, WarehousesCreate];
}
