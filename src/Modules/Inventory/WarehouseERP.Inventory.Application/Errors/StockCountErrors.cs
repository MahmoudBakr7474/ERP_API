using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Errors;

// Application rules about stock counts and the freeze they hold; the count invariants are StockCountDomainErrors.
public static class StockCountErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Inventory.StockCount.NotFound", "The stock count does not exist.");

    public static readonly Error AccessDenied = Error.Forbidden(
        "Inventory.StockCount.AccessDenied", "You are not allowed to access this count's warehouse.");

    public static readonly Error ScopeOverlaps = Error.Conflict(
        "Inventory.StockCount.ScopeOverlaps", "Another count already freezes part of this scope.");

    public static readonly Error StockChangedDuringCount = Error.Conflict(
        "Inventory.StockCount.StockChangedDuringCount",
        "Stock in the count's scope no longer matches its snapshot; cancel the count and start a new one.");

    public static Error ScopeFrozen(string countReference) => Error.BusinessRule(
        "Inventory.StockCount.ScopeFrozen", $"Count {countReference} has frozen this stock; nothing posts there until it is completed or cancelled.");

    public static Error LocationNotFound(int line) => Error.BusinessRule(
        "Inventory.StockCount.LocationNotFound", $"Line {line}: the location is not an active location of the count's warehouse.");
}
