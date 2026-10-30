using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

public static class CashAccountDomainErrors
{
    public static readonly Error CodeInvalid = Error.BusinessRule(
        "Accounting.CashAccount.CodeInvalid", $"A cashbox or bank code is 1 to {CashAccount.MaxCodeLength} upper-case letters, digits or dashes.");

    public static readonly Error NameInvalid = Error.BusinessRule(
        "Accounting.CashAccount.NameInvalid", $"A cashbox or bank name is 1 to {CashAccount.MaxNameLength} characters.");

    public static readonly Error KindInvalid = Error.BusinessRule(
        "Accounting.CashAccount.KindInvalid", "A treasury account is a cashbox or a bank account.");

    public static readonly Error AccountInvalid = Error.BusinessRule(
        "Accounting.CashAccount.AccountInvalid", "A cashbox or bank account is kept on an active asset posting account that is not a customer or supplier account.");

    public static readonly Error BankDetailsOnCashbox = Error.BusinessRule(
        "Accounting.CashAccount.BankDetailsOnCashbox", "A cashbox has no bank name, account number or overdraft.");

    public static readonly Error PosCashNotCashbox = Error.BusinessRule(
        "Accounting.CashAccount.PosCashNotCashbox", "Only a cashbox receives the cash of closed POS shifts.");

    public static readonly Error BankDetailsTooLong = Error.BusinessRule(
        "Accounting.CashAccount.BankDetailsTooLong", $"The bank name is at most {CashAccount.MaxBankNameLength} characters and the account number {CashAccount.MaxAccountNumberLength}.");
}
