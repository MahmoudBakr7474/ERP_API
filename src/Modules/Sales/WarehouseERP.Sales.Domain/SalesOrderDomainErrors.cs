using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

public static class SalesOrderDomainErrors
{
    public static readonly Error NotDraft = Error.BusinessRule(
        "Sales.Order.NotDraft", "Only a draft order can be changed, submitted or confirmed.");

    public static readonly Error NotSubmitted = Error.BusinessRule(
        "Sales.Order.NotSubmitted", "Only a submitted order can be approved or rejected.");

    public static readonly Error NotConfirmed = Error.BusinessRule(
        "Sales.Order.NotConfirmed", "The order is not confirmed.");

    public static readonly Error NotOpen = Error.BusinessRule(
        "Sales.Order.NotOpen", "Only a confirmed or partially delivered order delivers goods or takes reductions.");

    public static readonly Error NotCancellable = Error.BusinessRule(
        "Sales.Order.NotCancellable", "Only an order with nothing delivered can be cancelled; close what remains instead.");

    public static readonly Error NotClosable = Error.BusinessRule(
        "Sales.Order.NotClosable", "Only a partially delivered order can be closed.");

    public static readonly Error NothingDeliveredToClose = Error.BusinessRule(
        "Sales.Order.NothingDeliveredToClose", "Nothing was delivered yet: cancel the order instead of closing it.");

    public static readonly Error CancelInstead = Error.BusinessRule(
        "Sales.Order.CancelInstead", "This would take everything off an order with nothing delivered: cancel it instead.");

    public static readonly Error NoLines = Error.BusinessRule(
        "Sales.Order.NoLines", "An order needs at least one line.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Sales.Order.TooManyLines", "An order holds at most 200 lines.");

    public static readonly Error DuplicateLine = Error.BusinessRule(
        "Sales.Order.DuplicateLine", "An order line is named twice.");

    public static readonly Error PaymentTermsInvalid = Error.BusinessRule(
        "Sales.Order.PaymentTermsInvalid", "Payment terms run from 0 to 365 days.");

    public static readonly Error RequestedDateInvalid = Error.BusinessRule(
        "Sales.Order.RequestedDateInvalid", "The requested delivery date can't be before the order date.");

    public static Error LineNotFound(int line) => Error.BusinessRule(
        "Sales.Order.LineNotFound", $"Line {line}: the order has no such line.");

    public static Error ReductionInvalid(decimal remaining) => Error.BusinessRule(
        "Sales.Order.ReductionInvalid",
        string.Create(CultureInfo.InvariantCulture, $"A reduction is above 0 and at most what remains to deliver ({remaining})."));

    public static Error OverDelivery(int line, decimal remaining) => Error.BusinessRule(
        "Sales.Order.OverDelivery",
        string.Create(CultureInfo.InvariantCulture, $"Line {line}: only {remaining} remain to be delivered on this order line."));

    public static Error InvoiceExceedsDeliveries(int line, decimal invoiceable) => Error.BusinessRule(
        "Sales.Order.InvoiceExceedsDeliveries",
        string.Create(CultureInfo.InvariantCulture, $"Line {line}: only {invoiceable} delivered on this order line remain to be invoiced."));

    public static Error ReturnExceedsDeliveries(int line, decimal returnable) => Error.BusinessRule(
        "Sales.Order.ReturnExceedsDeliveries",
        string.Create(CultureInfo.InvariantCulture, $"Line {line}: only {returnable} delivered on this order line can still come back."));
}
