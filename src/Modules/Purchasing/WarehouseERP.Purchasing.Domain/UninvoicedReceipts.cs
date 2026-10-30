namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// The value of one order line's goods received and not invoiced: its uninvoiced quantity (received, less what invoices
/// billed and what went back before invoicing) at the order's net unit price, rounded once for the whole quantity. Each
/// receipt, invoice or return moves the line from one such value to the next, so the moves of a line always add up to the
/// value of what is left: exactly zero once everything received is invoiced or returned, however it was split.
/// </summary>
public static class UninvoicedReceipts
{
    public static decimal Value(decimal uninvoicedQuantity, decimal netUnitPrice, int currencyDecimals) =>
        Amounts.Round(uninvoicedQuantity * netUnitPrice, currencyDecimals);

    /// <summary>The change of value when the uninvoiced quantity goes from <paramref name="before"/> by <paramref name="change"/>.</summary>
    public static decimal Change(decimal before, decimal change, decimal netUnitPrice, int currencyDecimals) =>
        Value(before + change, netUnitPrice, currencyDecimals) - Value(before, netUnitPrice, currencyDecimals);
}
