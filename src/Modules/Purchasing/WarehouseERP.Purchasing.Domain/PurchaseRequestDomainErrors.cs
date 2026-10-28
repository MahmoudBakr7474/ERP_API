using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

public static class PurchaseRequestDomainErrors
{
    public static readonly Error NotDraft = Error.BusinessRule(
        "Purchasing.PurchaseRequest.NotDraft", "Only a draft request can be changed or submitted.");

    public static readonly Error NotSubmitted = Error.BusinessRule(
        "Purchasing.PurchaseRequest.NotSubmitted", "Only a submitted request can be approved or rejected.");

    public static readonly Error ApprovalRequired = Error.BusinessRule(
        "Purchasing.PurchaseRequest.ApprovalRequired", "This request needs approval: submit it.");

    public static readonly Error ApprovalNotRequired = Error.BusinessRule(
        "Purchasing.PurchaseRequest.ApprovalNotRequired", "This request needs no approval: release it.");

    public static readonly Error NotCancellable = Error.BusinessRule(
        "Purchasing.PurchaseRequest.NotCancellable", "Only a request nothing was ordered from can be cancelled; close it instead.");

    public static readonly Error NotClosable = Error.BusinessRule(
        "Purchasing.PurchaseRequest.NotClosable", "Only an approved request that is not fully ordered can be closed.");

    public static readonly Error NoLines = Error.BusinessRule(
        "Purchasing.PurchaseRequest.NoLines", "Add at least one line.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Purchasing.PurchaseRequest.TooManyLines", "A request holds at most 200 lines.");

    public static Error LineNotFound(int orderLine) => Error.BusinessRule(
        "Purchasing.PurchaseRequest.LineNotFound", $"Order line {orderLine}: the request line does not exist.");

    public static Error NotOrderable(int orderLine) => Error.BusinessRule(
        "Purchasing.PurchaseRequest.NotOrderable", $"Order line {orderLine}: its request is not approved, or is closed or fully ordered.");

    public static Error ExceedsRequest(int orderLine, decimal remaining) => Error.BusinessRule(
        "Purchasing.PurchaseRequest.ExceedsRequest",
        string.Create(CultureInfo.InvariantCulture, $"Order line {orderLine}: only {remaining} of the request line remain to be ordered."));
}
