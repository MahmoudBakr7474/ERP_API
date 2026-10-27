namespace WarehouseERP.Organization.Contracts;

/// <summary>
/// Permissions Organization endpoints require. Public so the host can register policies and a
/// future Identity module can grant them.
/// </summary>
public static class OrganizationPermissions
{
    /// <summary>Set up the company and change its settings (company-wide users only).</summary>
    public const string CompanyUpdate = "organization.company.update";

    /// <summary>Read branches (company-wide users see all, others the branches of their warehouses).</summary>
    public const string BranchesRead = "organization.branches.read";

    /// <summary>Create branches (company-wide users only).</summary>
    public const string BranchesCreate = "organization.branches.create";

    /// <summary>Rename, activate and deactivate branches (company-wide users only).</summary>
    public const string BranchesUpdate = "organization.branches.update";

    /// <summary>Read warehouses and their locations (still limited to the user's warehouse scope).</summary>
    public const string WarehousesRead = "organization.warehouses.read";

    /// <summary>Create warehouses (company-wide users only).</summary>
    public const string WarehousesCreate = "organization.warehouses.create";

    /// <summary>Change, activate and deactivate warehouses in the user's scope.</summary>
    public const string WarehousesUpdate = "organization.warehouses.update";

    /// <summary>Add locations to warehouses in the user's scope.</summary>
    public const string LocationsCreate = "organization.locations.create";

    /// <summary>Rename, activate and deactivate locations in warehouses in the user's scope.</summary>
    public const string LocationsUpdate = "organization.locations.update";

    /// <summary>Every Organization permission.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        CompanyUpdate, BranchesRead, BranchesCreate, BranchesUpdate, WarehousesRead, WarehousesCreate, WarehousesUpdate,
        LocationsCreate, LocationsUpdate,
    ];
}
