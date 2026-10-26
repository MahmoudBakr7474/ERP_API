using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Errors;

// Application rules about products; the product invariants themselves are ProductDomainErrors.
public static class ProductErrors
{
    public static readonly Error SkuTaken = Error.Conflict(
        "Catalog.Product.SkuTaken", "Another product already uses this SKU.");

    public static readonly Error NotFound = Error.NotFound(
        "Catalog.Product.NotFound", "The product does not exist.");
}
