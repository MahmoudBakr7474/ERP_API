using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class SourcePostingErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Accounting.Source.NotFound", "The operational document's posting record does not exist.");

    public static readonly Error NotActive = Error.BusinessRule(
        "Accounting.Source.NotActive", "Automatic accounting is not active yet: check the cutover and activate it first.");

    public static readonly Error Busy = Error.Conflict(
        "Accounting.Source.Busy", "Another run is journaling this document; try again shortly.");

    public static Error DocumentNotFound(SourceDocumentType type, string number) => Error.BusinessRule(
        "Accounting.Source.DocumentNotFound", $"{type} {number} is not posted in its module.");

    public static Error UnsupportedDocument(string number) => Error.BusinessRule(
        "Accounting.Source.UnsupportedDocument", $"Stock document {number} is of a kind the ledger has no rule for.");

    public static Error PosCashboxMissing(string shiftNumber) => Error.BusinessRule(
        "Accounting.Source.PosCashboxMissing",
        $"The branch of shift {shiftNumber} has no active cashbox receiving POS cash: mark one, then retry.");

    public static Error Unexpected(string exception) => Error.BusinessRule(
        "Accounting.Source.Unexpected", $"Journaling failed unexpectedly ({exception}); the server log has the details.");
}
