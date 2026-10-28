using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Errors;

// Stock levels and the locks that guard them.
public static class StockErrors
{
    public static readonly Error WarehouseAccessDenied = Error.Forbidden(
        "Inventory.Warehouse.AccessDenied", "You are not allowed to access this warehouse.");

    public static readonly Error Busy = Error.Conflict(
        "Inventory.Stock.Busy", "Another operation is changing this stock right now; try again.");

    public static Error Insufficient(string sku) => Error.BusinessRule(
        "Inventory.Stock.Insufficient", $"Not enough available stock of {sku} in this warehouse.");

    public static Error InsufficientAtLocation(string sku, string locationCode) => Error.BusinessRule(
        "Inventory.Stock.InsufficientAtLocation", $"Not enough stock of {sku} in location {locationCode}.");
}
