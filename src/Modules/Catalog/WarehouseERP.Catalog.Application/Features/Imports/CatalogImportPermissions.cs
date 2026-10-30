using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Imports;

/// <summary>
/// What the importing user may change, from the same permissions the single-record endpoints require: units need
/// units.manage, categories categories.manage, new products (with their units and barcodes) products.create, and
/// changes to existing products (names, category, added units or barcodes) products.update.
/// </summary>
public sealed record CatalogImportPermissions(bool Units, bool Categories, bool CreateProducts, bool UpdateProducts)
{
    public bool Any => Units || Categories || CreateProducts || UpdateProducts;

    public static CatalogImportPermissions Of(ICurrentUser user) => new(
        user.HasPermission(CatalogPermissions.UnitsManage),
        user.HasPermission(CatalogPermissions.CategoriesManage),
        user.HasPermission(CatalogPermissions.ProductsCreate),
        user.HasPermission(CatalogPermissions.ProductsUpdate));
}
