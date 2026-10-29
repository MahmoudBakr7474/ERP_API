using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Domain;

public static class PosReturnDomainErrors
{
    public static readonly Error OtherWarehouse = Error.BusinessRule(
        "Pos.Return.OtherWarehouse", "Goods come back at a terminal of the warehouse they were sold from.");

    public static readonly Error NoLines = Error.BusinessRule("Pos.Return.NoLines", "Choose at least one sold line to take back.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Pos.Return.TooManyLines", $"A return has at most {PosSale.MaxLines} lines.");

    public static readonly Error ReasonRequired = Error.BusinessRule(
        "Pos.Return.ReasonRequired", $"Say why the goods come back, in at most {PosReturn.MaxReasonLength} characters.");

    public static Error ConditionNotSupported(int line) => Error.BusinessRule(
        "Pos.Return.ConditionNotSupported",
        $"Line {line}: only goods fit for resale can be taken back for now; damaged or uninspected goods are refused until Inventory holds non-sellable stock.");

    public static Error RefundMismatch(decimal due, decimal entered) => Error.BusinessRule(
        "Pos.Refund.Mismatch",
        string.Create(CultureInfo.InvariantCulture, $"The refunds come to {entered}; the returned goods are worth {due}."));
}
