using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Errors;

public static class CategoryErrors
{
    public static readonly Error NotActive = Error.BusinessRule(
        "Catalog.Category.NotActive", "The category does not exist or is inactive.");
}
