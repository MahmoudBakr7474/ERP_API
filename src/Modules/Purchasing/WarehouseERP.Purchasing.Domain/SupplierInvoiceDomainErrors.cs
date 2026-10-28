using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

public static class SupplierInvoiceDomainErrors
{
    public static readonly Error OrderNotApproved = Error.BusinessRule(
        "Purchasing.SupplierInvoice.OrderNotApproved", "An invoice is recorded against an approved order only.");

    public static readonly Error NotDraft = Error.BusinessRule(
        "Purchasing.SupplierInvoice.NotDraft", "Only a draft invoice can be changed, submitted or posted directly.");

    public static readonly Error NotSubmitted = Error.BusinessRule(
        "Purchasing.SupplierInvoice.NotSubmitted", "Only a submitted invoice can be approved or rejected.");

    public static readonly Error NotOpen = Error.BusinessRule(
        "Purchasing.SupplierInvoice.NotOpen", "Only a draft or submitted invoice can be cancelled.");

    public static readonly Error ApprovalRequired = Error.BusinessRule(
        "Purchasing.SupplierInvoice.ApprovalRequired", "Its prices differ from the order: submit it for approval.");

    public static readonly Error ApprovalNotRequired = Error.BusinessRule(
        "Purchasing.SupplierInvoice.ApprovalNotRequired", "Its prices match the order: post it directly.");

    public static readonly Error NoLines = Error.BusinessRule(
        "Purchasing.SupplierInvoice.NoLines", "Add at least one line.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Purchasing.SupplierInvoice.TooManyLines", "An invoice holds at most 200 lines.");

    public static readonly Error DuplicateLine = Error.BusinessRule(
        "Purchasing.SupplierInvoice.DuplicateLine", "Each order line may be billed by one line of the invoice only.");

    public static readonly Error InvoiceNumberInvalid = Error.BusinessRule(
        "Purchasing.SupplierInvoice.InvoiceNumberInvalid", "The supplier invoice number needs 1 to 50 letters or digits.");

    public static readonly Error DueBeforeInvoice = Error.BusinessRule(
        "Purchasing.SupplierInvoice.DueBeforeInvoice", "The due date can't be before the invoice date.");

    public static Error OrderLineNotFound(int line) => Error.BusinessRule(
        "Purchasing.SupplierInvoice.OrderLineNotFound", $"Line {line}: it is not a line of the invoice's order.");
}
