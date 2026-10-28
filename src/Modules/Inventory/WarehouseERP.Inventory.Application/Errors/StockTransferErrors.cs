using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Errors;

// Application rules about transfers; the transfer invariants themselves are StockTransferDomainErrors.
public static class StockTransferErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Inventory.StockTransfer.NotFound", "The transfer does not exist.");

    public static readonly Error AccessDenied = Error.Forbidden(
        "Inventory.StockTransfer.AccessDenied", "You are not allowed to access this transfer's warehouses.");

    public static readonly Error PermissionDenied = Error.Forbidden(
        "Inventory.StockTransfer.PermissionDenied", "You are not allowed to do this with transfers.");

    public static readonly Error SourceNotUsable = Error.BusinessRule(
        "Inventory.StockTransfer.SourceNotUsable", "A transfer leaves an active physical warehouse.");

    public static readonly Error DestinationNotUsable = Error.BusinessRule(
        "Inventory.StockTransfer.DestinationNotUsable", "A transfer arrives in an active physical warehouse.");

    public static readonly Error TransitWarehouseRequired = Error.BusinessRule(
        "Inventory.StockTransfer.TransitWarehouseRequired",
        "Name the transit warehouse: the source branch has no single active transit warehouse.");

    public static readonly Error TransitWarehouseInvalid = Error.BusinessRule(
        "Inventory.StockTransfer.TransitWarehouseInvalid", "The transit warehouse must be an active warehouse of type Transit.");
}
