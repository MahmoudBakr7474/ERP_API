using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Domain;

public static class StockCountDomainErrors
{
    public static readonly Error TooManyLocations = Error.BusinessRule(
        "Inventory.StockCount.TooManyLocations", "A count covers the whole warehouse or at most 200 locations.");

    public static readonly Error NotDraft = Error.BusinessRule(
        "Inventory.StockCount.NotDraft", "Only a count that has not started can start.");

    public static readonly Error NotCounting = Error.BusinessRule(
        "Inventory.StockCount.NotCounting", "Counted quantities are entered, and the count submitted, only while it is counting.");

    public static readonly Error NotSubmitted = Error.BusinessRule(
        "Inventory.StockCount.NotSubmitted", "Only a submitted count can be approved or rejected.");

    public static readonly Error NotOpen = Error.BusinessRule(
        "Inventory.StockCount.NotOpen", "A completed or cancelled count cannot change.");

    public static readonly Error NotFullyCounted = Error.BusinessRule(
        "Inventory.StockCount.NotFullyCounted", "Enter a counted quantity for every line before submitting.");

    public static readonly Error SelfApproval = Error.BusinessRule(
        "Inventory.StockCount.SelfApproval", "Nobody may approve a count they created.");

    public static readonly Error OutOfScope = Error.BusinessRule(
        "Inventory.StockCount.OutOfScope", "That location is not part of this count.");

    public static readonly Error QuantityMustBeWhole = Error.BusinessRule(
        "Inventory.StockCount.QuantityMustBeWhole", "The base unit cannot be split; count a whole quantity.");
}
