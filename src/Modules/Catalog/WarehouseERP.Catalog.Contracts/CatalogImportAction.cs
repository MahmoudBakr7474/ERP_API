namespace WarehouseERP.Catalog.Contracts;

/// <summary>What confirming an import will do with one row.</summary>
public enum CatalogImportAction : byte
{
    /// <summary>A new record is created.</summary>
    Create = 1,

    /// <summary>The existing record's editable fields change.</summary>
    Update = 2,

    /// <summary>The record exists and already matches the row (Update mode): nothing changes.</summary>
    Unchanged = 3,

    /// <summary>The record exists and Skip mode leaves it alone.</summary>
    Skip = 4,

    /// <summary>The row has errors; while any row has, nothing is imported.</summary>
    Error = 5,
}
