using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

public static class VoucherDomainErrors
{
    public static readonly Error CashRequired = Error.BusinessRule(
        "Accounting.Voucher.CashRequired", "Choose the cashbox or bank account the money goes through.");

    public static readonly Error CashNotAllowed = Error.BusinessRule(
        "Accounting.Voucher.CashNotAllowed", "An expense bill moves no money; it is paid later by a supplier payment.");

    public static readonly Error CounterCashRequired = Error.BusinessRule(
        "Accounting.Voucher.CounterCashRequired", "A transfer needs the cashbox or bank account the money goes to.");

    public static readonly Error CounterCashNotAllowed = Error.BusinessRule(
        "Accounting.Voucher.CounterCashNotAllowed", "Only a transfer names a second cashbox or bank account.");

    public static readonly Error SameCashAccount = Error.BusinessRule(
        "Accounting.Voucher.SameCashAccount", "A transfer moves money between two different cashboxes or bank accounts.");

    public static readonly Error PartyRequired = Error.BusinessRule(
        "Accounting.Voucher.PartyRequired", "Choose the customer or supplier of this voucher.");

    public static readonly Error PartyNotAllowed = Error.BusinessRule(
        "Accounting.Voucher.PartyNotAllowed", "Cash vouchers and transfers name no customer or supplier; use a receipt, payment or refund.");

    public static Error PartyTypeMismatch(PartyType expected) => Error.BusinessRule(
        "Accounting.Voucher.PartyTypeMismatch", $"This voucher's party is a {expected}.");

    public static readonly Error MethodRequired = Error.BusinessRule(
        "Accounting.Voucher.MethodRequired", "Say how the money moved: cash, bank transfer, cleared cheque or card.");

    public static readonly Error MethodDoesNotFitCash = Error.BusinessRule(
        "Accounting.Voucher.MethodDoesNotFitCash", "Cash goes through a cashbox; transfers, cleared cheques and card settlements through a bank account.");

    public static readonly Error MethodNotAllowed = Error.BusinessRule(
        "Accounting.Voucher.MethodNotAllowed", "Only receipts, payments, refunds and cash vouchers record a payment method.");

    public static readonly Error ChequeNumberRequired = Error.BusinessRule(
        "Accounting.Voucher.ChequeNumberRequired", "A cleared cheque needs its number in the reference.");

    public static readonly Error WithholdingInvalid = Error.BusinessRule(
        "Accounting.Voucher.WithholdingInvalid", "Only customer receipts and supplier payments carry tax withheld, never below zero.");

    public static readonly Error TaxInvalid = Error.BusinessRule(
        "Accounting.Voucher.TaxInvalid", "Only an expense bill carries VAT, never below zero.");

    public static readonly Error AmountInvalid = Error.BusinessRule(
        "Accounting.Voucher.AmountInvalid", "The amount is above zero (a receipt or payment may be tax withheld only).");

    public static readonly Error LinesRequired = Error.BusinessRule(
        "Accounting.Voucher.LinesRequired", "Cash vouchers and expense bills need at least one counter line.");

    public static readonly Error LinesNotAllowed = Error.BusinessRule(
        "Accounting.Voucher.LinesNotAllowed", "Receipts, payments, refunds and transfers take an amount, not counter lines.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Accounting.Voucher.TooManyLines", $"A voucher has at most {VoucherLines.MaxLines} counter lines.");

    public static Error LineAmountInvalid(int line) => Error.BusinessRule(
        "Accounting.Voucher.LineAmountInvalid", $"Counter line {line}: the amount is above zero.");

    public static readonly Error BillNumberRequired = Error.BusinessRule(
        "Accounting.Voucher.BillNumberRequired", "An expense bill needs the supplier's bill number in the reference.");

    public static readonly Error DueBeforeBill = Error.BusinessRule(
        "Accounting.Voucher.DueBeforeBill", "An expense bill falls due on or after its date.");

    public static readonly Error AllocationsNotAllowed = Error.BusinessRule(
        "Accounting.Voucher.AllocationsNotAllowed", "Only receipts, payments and refunds settle open items.");

    public static readonly Error AllocationInvalid = Error.BusinessRule(
        "Accounting.Voucher.AllocationInvalid", "Each open item is settled once, by an amount above zero.");

    public static readonly Error AllocationsExceedAmount = Error.BusinessRule(
        "Accounting.Voucher.AllocationsExceedAmount", "The open items settled add up to more than the voucher.");
}
