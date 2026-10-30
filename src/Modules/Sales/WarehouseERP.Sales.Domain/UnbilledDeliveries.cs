namespace WarehouseERP.Sales.Domain;

/// <summary>
/// The value of one order line's goods delivered and not invoiced: its uninvoiced quantity (delivered, less what invoices
/// billed and what came back before invoicing) priced like an invoice line (<see cref="LineAmounts"/>: agreed price and
/// discount, net of VAT), rounded once for the whole quantity. Each delivery, invoice or return moves the line from one such
/// value to the next, so the moves of a line always add up to the value of what is left: exactly zero once everything
/// delivered is invoiced or returned, however it was split.
/// </summary>
public static class UnbilledDeliveries
{
    public static decimal Value(decimal uninvoicedQuantity, LinePrice price, int currencyDecimals)
    {
        if (uninvoicedQuantity == 0m)
            return 0m;
        var amounts = LineAmounts.Calculate(Math.Abs(uninvoicedQuantity), price, currencyDecimals, line: 0);
        if (amounts.IsFailure)
            throw new InvalidOperationException($"An order line's agreed price can't be valued: {amounts.Error.Code}");
        return uninvoicedQuantity < 0m ? -amounts.Value.Net : amounts.Value.Net;
    }

    /// <summary>The change of value when the uninvoiced quantity goes from <paramref name="before"/> by <paramref name="change"/>.</summary>
    public static decimal Change(decimal before, decimal change, LinePrice price, int currencyDecimals) =>
        Value(before + change, price, currencyDecimals) - Value(before, price, currencyDecimals);
}
