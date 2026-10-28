using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Domain;

public static class StockDocumentDomainErrors
{
    public static readonly Error NotDraft = Error.BusinessRule(
        "Inventory.StockDocument.NotDraft", "Only a draft document can be changed, submitted or posted directly.");

    public static readonly Error NotSubmitted = Error.BusinessRule(
        "Inventory.StockDocument.NotSubmitted", "Only a submitted document can be approved or rejected.");

    public static readonly Error NotOpen = Error.BusinessRule(
        "Inventory.StockDocument.NotOpen", "Only a draft or submitted document can be cancelled.");

    public static readonly Error NotPosted = Error.BusinessRule(
        "Inventory.StockDocument.NotPosted", "Only a posted document can be reversed.");

    public static readonly Error ApprovalRequired = Error.BusinessRule(
        "Inventory.StockDocument.ApprovalRequired", "This document needs approval: submit it instead of posting it.");

    public static readonly Error ApprovalNotRequired = Error.BusinessRule(
        "Inventory.StockDocument.ApprovalNotRequired", "This document needs no approval: post it directly.");

    public static readonly Error SelfApproval = Error.BusinessRule(
        "Inventory.StockDocument.SelfApproval", "Nobody may approve a document they created.");

    public static readonly Error NoLines = Error.BusinessRule(
        "Inventory.StockDocument.NoLines", "A document needs at least one line.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Inventory.StockDocument.TooManyLines", "A document holds at most 500 lines.");

    public static readonly Error ReasonRequired = Error.BusinessRule(
        "Inventory.StockDocument.ReasonRequired", "An adjustment needs a reason.");

    public static readonly Error ReasonOnlyForAdjustments = Error.BusinessRule(
        "Inventory.StockDocument.ReasonOnlyForAdjustments", "Only adjustments carry a reason.");

    public static readonly Error ReversalLinesFixed = Error.BusinessRule(
        "Inventory.StockDocument.ReversalLinesFixed", "A reversal mirrors the posted document; its lines cannot change.");

    public static readonly Error CannotReverseReversal = Error.BusinessRule(
        "Inventory.StockDocument.CannotReverseReversal", "A reversal cannot be reversed; post a new document instead.");

    public static Error WrongDirection(int line) => Error.BusinessRule(
        "Inventory.StockDocument.WrongDirection", $"Line {line}: this document type cannot move stock that way.");

    public static Error UnitCostRequired(int line) => Error.BusinessRule(
        "Inventory.StockDocument.UnitCostRequired", $"Line {line}: opening stock and receipts need a unit cost.");

    public static Error UnitCostNotAllowed(int line) => Error.BusinessRule(
        "Inventory.StockDocument.UnitCostNotAllowed", $"Line {line}: stock going out is valued at the average cost; give no unit cost.");

    public static Error QuantityMustBeWhole(int line) => Error.BusinessRule(
        "Inventory.StockDocument.QuantityMustBeWhole", $"Line {line}: this unit cannot be split; enter a whole quantity.");

    public static Error ConversionNotExact(int line) => Error.BusinessRule(
        "Inventory.StockDocument.ConversionNotExact",
        $"Line {line}: the quantity does not convert exactly to the base unit (whole when it cannot be split, otherwise at most 4 decimals).");

    public static Error UnitCostTooLarge(int line) => Error.BusinessRule(
        "Inventory.StockDocument.UnitCostTooLarge", $"Line {line}: the unit cost or the line's value is too large.");

    public static Error QuantityTooLarge(int line) => Error.BusinessRule(
        "Inventory.StockDocument.QuantityTooLarge", $"Line {line}: the quantity is too large.");
}
