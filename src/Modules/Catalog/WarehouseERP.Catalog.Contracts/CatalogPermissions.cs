namespace WarehouseERP.Catalog.Contracts;

/// <summary>
/// Permissions Catalog endpoints require. Public so the host can register policies and a future
/// Identity module can grant them.
/// </summary>
public static class CatalogPermissions
{
    /// <summary>Read products.</summary>
    public const string ProductsRead = "catalog.products.read";

    /// <summary>Create products.</summary>
    public const string ProductsCreate = "catalog.products.create";

    /// <summary>Change products.</summary>
    public const string ProductsUpdate = "catalog.products.update";

    /// <summary>Every Catalog permission.</summary>
    public static IReadOnlyList<string> All { get; } = [ProductsRead, ProductsCreate, ProductsUpdate];
}
