using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Errors;

// The product unit invariants themselves are ProductUnitDomainErrors.
public static class ProductUnitErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Catalog.ProductUnit.NotFound", "The product has no such unit.");
}
