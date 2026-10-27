using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Errors;

public static class CategoryErrors
{
    public static readonly Error NotActive = Error.BusinessRule(
        "Catalog.Category.NotActive", "The category does not exist or is inactive.");

    public static readonly Error CodeTaken = Error.Conflict(
        "Catalog.Category.CodeTaken", "Another category already uses this code.");

    public static readonly Error NotFound = Error.NotFound(
        "Catalog.Category.NotFound", "The category does not exist.");

    public static readonly Error ParentNotActive = Error.BusinessRule(
        "Catalog.Category.ParentNotActive", "The parent category does not exist or is inactive.");

    public static readonly Error Cycle = Error.BusinessRule(
        "Catalog.Category.Cycle", "A category can't be placed under itself or one of its subcategories.");

    // An inactive category never holds an active subcategory.
    public static readonly Error HasActiveChildren = Error.BusinessRule(
        "Catalog.Category.HasActiveChildren", "The category still has active subcategories; deactivate them first.");
}
