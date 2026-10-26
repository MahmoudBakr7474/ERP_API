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
}
