using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

public static class SupplierReturnDomainErrors
{
    public static readonly Error NotDraft = Error.BusinessRule(
        "Purchasing.SupplierReturn.NotDraft", "Only a draft return can be changed or submitted.");

    public static readonly Error NotSubmitted = Error.BusinessRule(
        "Purchasing.SupplierReturn.NotSubmitted", "Only a submitted return can be approved or rejected.");

    public static readonly Error NotOpen = Error.BusinessRule(
        "Purchasing.SupplierReturn.NotOpen", "Only a draft or submitted return can be cancelled.");

    public static readonly Error NoLines = Error.BusinessRule(
        "Purchasing.SupplierReturn.NoLines", "Add at least one line.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Purchasing.SupplierReturn.TooManyLines", "A return holds at most 200 lines.");

    public static readonly Error DuplicateLine = Error.BusinessRule(
        "Purchasing.SupplierReturn.DuplicateLine", "Each receipt line may appear only once.");

    public static Error ReceiptLineNotFound(int line) => Error.BusinessRule(
        "Purchasing.SupplierReturn.ReceiptLineNotFound", $"Line {line}: it is not a line of the goods receipt.");

    public static Error ExceedsReceipt(int line, decimal returnable) => Error.BusinessRule(
        "Purchasing.SupplierReturn.ExceedsReceipt",
        string.Create(CultureInfo.InvariantCulture, $"Line {line}: only {returnable} of what this receipt brought in may still be returned."));
}
