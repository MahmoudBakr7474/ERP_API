using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Errors;

// Application rules about warehouses; the warehouse invariants themselves are WarehouseDomainErrors.
public static class WarehouseErrors
{
    public static readonly Error CodeTaken = Error.Conflict(
        "Organization.Warehouse.CodeTaken", "Another warehouse in this branch already uses this code.");

    public static readonly Error NotFound = Error.NotFound(
        "Organization.Warehouse.NotFound", "The warehouse does not exist.");

    public static readonly Error AccessDenied = Error.Forbidden(
        "Organization.Warehouse.AccessDenied", "You are not allowed to access this warehouse.");

    public static readonly Error HoldsStock = Error.BusinessRule(
        "Organization.Warehouse.HoldsStock", "The warehouse still holds stock; move or adjust it out first.");

    public static readonly Error HasOpenInventoryDocuments = Error.BusinessRule(
        "Organization.Warehouse.HasOpenInventoryDocuments",
        "Inventory documents waiting for approval, open transfers or a running count still need this warehouse.");

    public static readonly Error InventoryBusy = Error.Conflict(
        "Organization.Warehouse.InventoryBusy", "Stock in this warehouse is being changed right now; try again.");
}
