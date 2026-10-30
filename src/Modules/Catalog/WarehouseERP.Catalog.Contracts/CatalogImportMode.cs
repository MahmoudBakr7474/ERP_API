namespace WarehouseERP.Catalog.Contracts;

/// <summary>
/// What a catalog import does with a row whose code (unit or category code, SKU, product unit, barcode) already exists.
/// New rows are always created. An import never deletes, deactivates or reactivates anything.
/// </summary>
public enum CatalogImportMode : byte
{
    /// <summary>Only new records: a row whose code exists is an error and nothing is imported.</summary>
    Create = 1,

    /// <summary>New records are created and existing ones updated where their editable fields differ (names, category,
    /// a unit's fraction setting). Fixed fields (base unit, type, tracking, factor, barcode type, parent) must match.</summary>
    Update = 2,

    /// <summary>New records are created; existing ones are left exactly as they are.</summary>
    Skip = 3,
}
