namespace WarehouseERP.Catalog.Contracts;

/// <summary>
/// Permissions Catalog endpoints require. Public so the host can register policies and the Identity module can
/// grant them. Catalog data is shared by the whole company, so these permissions carry no branch or warehouse scope.
/// </summary>
public static class CatalogPermissions
{
    /// <summary>Read products, units and categories, and look products up by barcode.</summary>
    public const string ProductsRead = "catalog.products.read";

    /// <summary>Create products.</summary>
    public const string ProductsCreate = "catalog.products.create";

    /// <summary>Change products: names, category, status, their units and barcodes.</summary>
    public const string ProductsUpdate = "catalog.products.update";

    /// <summary>Create, change, activate and deactivate units of measure.</summary>
    public const string UnitsManage = "catalog.units.manage";

    /// <summary>Create, change, move, activate and deactivate categories.</summary>
    public const string CategoriesManage = "catalog.categories.manage";

    /// <summary>Every Catalog permission.</summary>
    public static IReadOnlyList<string> All { get; } =
        [ProductsRead, ProductsCreate, ProductsUpdate, UnitsManage, CategoriesManage];
}
