namespace WarehouseERP.Organization.Contracts;

/// <summary>What confirming a location import will do with one row.</summary>
public enum LocationImportAction : byte
{
    /// <summary>A new location is added to the warehouse.</summary>
    Create = 1,

    /// <summary>The existing location's names change.</summary>
    Update = 2,

    /// <summary>The location exists and already has these names (Update mode): nothing changes.</summary>
    Unchanged = 3,

    /// <summary>The location exists and Skip mode leaves it alone.</summary>
    Skip = 4,

    /// <summary>The row has errors; while any row has, nothing is imported.</summary>
    Error = 5,
}
