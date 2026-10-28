using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

public static class PurchaseOrderDomainErrors
{
    public static readonly Error NotDraft = Error.BusinessRule(
        "Purchasing.PurchaseOrder.NotDraft", "Only a draft order can be changed or submitted.");

    public static readonly Error NotSubmitted = Error.BusinessRule(
        "Purchasing.PurchaseOrder.NotSubmitted", "Only a submitted order can be approved or rejected.");

    public static readonly Error ApprovalRequired = Error.BusinessRule(
        "Purchasing.PurchaseOrder.ApprovalRequired", "This order needs approval: submit it.");

    public static readonly Error ApprovalNotRequired = Error.BusinessRule(
        "Purchasing.PurchaseOrder.ApprovalNotRequired", "This order needs no approval: release it.");

    public static readonly Error NotCancellable = Error.BusinessRule(
        "Purchasing.PurchaseOrder.NotCancellable", "Only an order nothing was received against can be cancelled; close it instead.");

    public static readonly Error NothingReceivedToClose = Error.BusinessRule(
        "Purchasing.PurchaseOrder.NothingReceivedToClose", "Nothing was received against this order: cancel it instead of closing it.");

    public static readonly Error NotClosable = Error.BusinessRule(
        "Purchasing.PurchaseOrder.NotClosable", "Only a partially received order can be closed.");

    public static readonly Error NotReceivable = Error.BusinessRule(
        "Purchasing.PurchaseOrder.NotReceivable", "Goods are received only against an approved order that is not yet complete or closed.");

    public static readonly Error NotApproved = Error.BusinessRule(
        "Purchasing.PurchaseOrder.NotApproved", "The order is not approved.");

    public static readonly Error NoLines = Error.BusinessRule(
        "Purchasing.PurchaseOrder.NoLines", "Add at least one line.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Purchasing.PurchaseOrder.TooManyLines", "An order holds at most 200 lines.");

    public static readonly Error DuplicateLine = Error.BusinessRule(
        "Purchasing.PurchaseOrder.DuplicateLine", "Each order line may appear only once.");

    public static readonly Error PaymentTermsInvalid = Error.BusinessRule(
        "Purchasing.PurchaseOrder.PaymentTermsInvalid", "Payment terms run from 0 to 365 days.");

    public static readonly Error RequestLineRepeated = Error.BusinessRule(
        "Purchasing.PurchaseOrder.RequestLineRepeated", "A purchase request line can be ordered by one line of the order only.");

    public static Error LineNotFound(int line) => Error.BusinessRule(
        "Purchasing.PurchaseOrder.LineNotFound", $"Line {line}: it is not a line of the order.");

    public static Error OverReceipt(int line, decimal remaining) => Error.BusinessRule(
        "Purchasing.PurchaseOrder.OverReceipt",
        string.Create(CultureInfo.InvariantCulture, $"Line {line}: only {remaining} remain to be received; nothing beyond the order is accepted."));

    public static Error InvoiceExceedsReceipts(int line, decimal invoiceable) => Error.BusinessRule(
        "Purchasing.PurchaseOrder.InvoiceExceedsReceipts",
        string.Create(CultureInfo.InvariantCulture, $"Line {line}: only {invoiceable} received and not yet invoiced may be invoiced."));

    public static Error ReturnExceedsReceipts(int line, decimal returnable) => Error.BusinessRule(
        "Purchasing.PurchaseOrder.ReturnExceedsReceipts",
        string.Create(CultureInfo.InvariantCulture, $"Line {line}: only {returnable} received and not yet returned may be returned."));
}
