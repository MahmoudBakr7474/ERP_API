using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Domain;

public static class StockTransferDomainErrors
{
    public static readonly Error SameWarehouse = Error.BusinessRule(
        "Inventory.StockTransfer.SameWarehouse", "A transfer goes from one warehouse to another.");

    public static readonly Error NoLines = Error.BusinessRule(
        "Inventory.StockTransfer.NoLines", "A transfer needs at least one line.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Inventory.StockTransfer.TooManyLines", "A transfer holds at most 500 lines.");

    public static readonly Error NotDraft = Error.BusinessRule(
        "Inventory.StockTransfer.NotDraft", "Only a draft transfer can be changed, submitted or dispatched directly.");

    public static readonly Error NotSubmitted = Error.BusinessRule(
        "Inventory.StockTransfer.NotSubmitted", "Only a submitted transfer can be approved or rejected.");

    public static readonly Error NotOpen = Error.BusinessRule(
        "Inventory.StockTransfer.NotOpen", "Only a draft or submitted transfer can be cancelled; a dispatched one is received or closed.");

    public static readonly Error ApprovalRequired = Error.BusinessRule(
        "Inventory.StockTransfer.ApprovalRequired", "This transfer needs approval: submit it instead of dispatching it.");

    public static readonly Error ApprovalNotRequired = Error.BusinessRule(
        "Inventory.StockTransfer.ApprovalNotRequired", "This transfer needs no approval: dispatch it directly.");

    public static readonly Error SelfApproval = Error.BusinessRule(
        "Inventory.StockTransfer.SelfApproval", "Nobody may approve a transfer they created.");

    public static readonly Error NotInTransit = Error.BusinessRule(
        "Inventory.StockTransfer.NotInTransit", "Only a dispatched transfer that is not being closed can receive stock or be closed.");

    public static readonly Error NothingOutstanding = Error.BusinessRule(
        "Inventory.StockTransfer.NothingOutstanding", "Nothing of this transfer is still in transit.");

    public static readonly Error NothingToReceive = Error.BusinessRule(
        "Inventory.StockTransfer.NothingToReceive", "Say which lines arrived and how much.");

    public static Error UnknownLine(Guid lineId) => Error.BusinessRule(
        "Inventory.StockTransfer.UnknownLine", $"Line {lineId} is not a line of this transfer.");

    public static Error ReceiptExceedsOutstanding(int line) => Error.BusinessRule(
        "Inventory.StockTransfer.ReceiptExceedsOutstanding", $"Line {line}: more is received than is still in transit.");

    public static Error QuantityMustBeWhole(int line) => Error.BusinessRule(
        "Inventory.StockTransfer.QuantityMustBeWhole", $"Line {line}: the base unit cannot be split; receive a whole quantity.");
}
