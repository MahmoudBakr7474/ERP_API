namespace WarehouseERP.Organization.Contracts;

/// <summary>
/// Permissions Organization endpoints require. Public so the host can register policies and a
/// future Identity module can grant them.
/// </summary>
public static class OrganizationPermissions
{
    /// <summary>Read warehouses (still limited to the user's warehouse scope).</summary>
    public const string WarehousesRead = "organization.warehouses.read";

    /// <summary>Create warehouses.</summary>
    public const string WarehousesCreate = "organization.warehouses.create";

    /// <summary>Every Organization permission.</summary>
    public static IReadOnlyList<string> All { get; } = [WarehousesRead, WarehousesCreate];
}
