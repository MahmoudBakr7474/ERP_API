namespace WarehouseERP.Catalog.Contracts;

/// <summary>
/// Permissions Catalog endpoints require. Public so the host can register policies and a future
/// Identity module can grant them.
/// </summary>
public static class CatalogPermissions
{
    public const string ProductsRead = "catalog.products.read";
    public const string ProductsCreate = "catalog.products.create";
    public const string ProductsUpdate = "catalog.products.update";

    public static IReadOnlyList<string> All { get; } = [ProductsRead, ProductsCreate, ProductsUpdate];
}
