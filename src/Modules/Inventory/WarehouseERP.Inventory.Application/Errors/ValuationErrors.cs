using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Errors;

public static class ValuationErrors
{
    public static Error UnitCostRequired(int line) => Error.BusinessRule(
        "Inventory.Valuation.UnitCostRequired", $"Line {line}: the product has no cost yet; give this increase a unit cost.");
}
