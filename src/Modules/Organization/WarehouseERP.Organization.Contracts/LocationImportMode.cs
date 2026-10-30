namespace WarehouseERP.Organization.Contracts;

/// <summary>
/// What a location import does with a row whose location (branch, warehouse and location code) already exists. New
/// rows are always created. An import never moves a location, never changes a code, never activates or deactivates.
/// </summary>
public enum LocationImportMode : byte
{
    /// <summary>Only new locations: a row whose location exists is an error and nothing is imported.</summary>
    Create = 1,

    /// <summary>New locations are created and existing ones renamed where their names differ. The default location
    /// is never changed.</summary>
    Update = 2,

    /// <summary>New locations are created; existing ones are left exactly as they are.</summary>
    Skip = 3,
}
