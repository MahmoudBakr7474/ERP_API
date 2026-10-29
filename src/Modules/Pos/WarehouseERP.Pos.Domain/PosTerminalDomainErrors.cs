using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Domain;

public static class PosTerminalDomainErrors
{
    public static readonly Error CodeInvalid = Error.BusinessRule(
        "Pos.Terminal.CodeInvalid", $"A terminal code is 1 to {PosTerminal.MaxCodeLength} upper-case letters, digits or dashes, starting with a letter or digit.");

    public static readonly Error NameInvalid = Error.BusinessRule(
        "Pos.Terminal.NameInvalid", $"A terminal name is 1 to {PosTerminal.MaxNameLength} characters.");

    public static readonly Error NoPaymentMethod = Error.BusinessRule(
        "Pos.Terminal.NoPaymentMethod", "A terminal accepts cash, card or both.");

    public static readonly Error ReceiptTextTooLong = Error.BusinessRule(
        "Pos.Terminal.ReceiptTextTooLong", $"Receipt header and footer are at most {PosTerminal.MaxReceiptTextLength} characters each.");
}
