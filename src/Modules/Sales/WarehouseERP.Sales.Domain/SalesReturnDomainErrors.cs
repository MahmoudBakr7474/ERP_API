using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

public static class SalesReturnDomainErrors
{
    public static readonly Error NotDraft = Error.BusinessRule(
        "Sales.Return.NotDraft", "Only a draft return can be changed or submitted.");

    public static readonly Error NotSubmitted = Error.BusinessRule(
        "Sales.Return.NotSubmitted", "Only a submitted return can be approved or rejected.");

    public static readonly Error NotCancellable = Error.BusinessRule(
        "Sales.Return.NotCancellable", "An approved or cancelled return can't be cancelled.");

    public static readonly Error NoLines = Error.BusinessRule(
        "Sales.Return.NoLines", "A return needs at least one line.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Sales.Return.TooManyLines", "A return holds at most 500 lines.");

    public static Error ConditionNotSupported(int line) => Error.BusinessRule(
        "Sales.Return.ConditionNotSupported",
        $"Line {line}: only goods confirmed fit for resale can be returned to stock for now; damaged or uninspected goods are not supported yet.");
}
