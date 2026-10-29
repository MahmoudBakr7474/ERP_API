using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Domain;

public static class PosSaleDomainErrors
{
    public static readonly Error NoLines = Error.BusinessRule("Pos.Sale.NoLines", "Scan at least one item.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Pos.Sale.TooManyLines", $"A sale has at most {PosSale.MaxLines} lines.");

    public static readonly Error CustomerTooLong = Error.BusinessRule(
        "Pos.Sale.CustomerTooLong",
        $"A walk-in customer's name is at most {PosSale.MaxCustomerNameLength} characters and phone at most {PosSale.MaxPhoneLength}.");

    public static readonly Error NotesTooLong = Error.BusinessRule(
        "Pos.Sale.NotesTooLong", $"Notes are at most {PosSale.MaxNotesLength} characters.");

    public static readonly Error TotalTooLarge = Error.BusinessRule("Pos.Sale.TotalTooLarge", "The sale's total is too large.");

    public static readonly Error NoPayment = Error.BusinessRule("Pos.Payment.Missing", "Enter how the customer pays.");

    public static readonly Error TooManyPayments = Error.BusinessRule(
        "Pos.Payment.TooMany", $"A sale or refund has at most {PosSale.MaxPayments} payment lines.");

    public static readonly Error OneCashPaymentOnly = Error.BusinessRule(
        "Pos.Payment.OneCashOnly", "Enter all the cash in one payment line.");

    public static readonly Error CardExceedsTotal = Error.BusinessRule(
        "Pos.Payment.CardExceedsTotal", "Card payments can't exceed the amount due; change is given from cash only.");

    public static readonly Error CashNotNeeded = Error.BusinessRule(
        "Pos.Payment.CashNotNeeded", "The card payments already cover the total; remove the cash payment.");

    public static readonly Error NotPosted = Error.BusinessRule("Pos.Sale.NotPosted", "The sale was never posted to stock.");

    public static Error PaymentMethodInvalid(int line) => Error.BusinessRule(
        "Pos.Payment.MethodInvalid", $"Payment {line}: pay by cash or card.");

    public static Error PaymentMethodNotAccepted(int line) => Error.BusinessRule(
        "Pos.Payment.MethodNotAccepted", $"Payment {line}: this terminal does not accept this payment method.");

    public static Error PaymentAmountInvalid(int line) => Error.BusinessRule(
        "Pos.Payment.AmountInvalid", $"Payment {line}: enter an amount above 0 with no more decimals than the currency has.");

    public static Error CardReferenceRequired(int line) => Error.BusinessRule(
        "Pos.Payment.CardReferenceRequired",
        $"Payment {line}: enter the card terminal's authorization or reference number (at most {PosSale.MaxReferenceLength} characters).");

    public static Error CardLast4Invalid(int line) => Error.BusinessRule(
        "Pos.Payment.CardLast4Invalid", $"Payment {line}: only the card's last four digits may be recorded.");

    public static Error Underpaid(decimal missing) => Error.BusinessRule(
        "Pos.Payment.Underpaid", string.Create(CultureInfo.InvariantCulture, $"The payments are {missing} short of the total."));

    public static Error LineNotFound(int line) => Error.BusinessRule(
        "Pos.Return.SaleLineNotFound", $"Line {line}: the sale has no such line.");

    public static Error ReturnQuantityInvalid(int line) => Error.BusinessRule(
        "Pos.Return.QuantityInvalid", $"Line {line}: return a quantity above 0 with at most 4 decimals.");

    public static Error ReturnExceedsSale(int line, decimal returnable) => Error.BusinessRule(
        "Pos.Return.ExceedsSale",
        string.Create(CultureInfo.InvariantCulture, $"Line {line}: at most {returnable} of this sale line can still come back."));

    public static Error CashRefundExceedsPaid(decimal available) => Error.BusinessRule(
        "Pos.Refund.CashExceedsPaid",
        string.Create(CultureInfo.InvariantCulture, $"Only {available} of this sale was paid in cash and not refunded yet."));

    public static Error CardRefundExceedsPaid(decimal available) => Error.BusinessRule(
        "Pos.Refund.CardExceedsPaid",
        string.Create(CultureInfo.InvariantCulture, $"Only {available} of this sale was paid by card and not refunded yet."));
}
