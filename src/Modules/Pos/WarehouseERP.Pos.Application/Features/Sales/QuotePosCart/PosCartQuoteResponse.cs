namespace WarehouseERP.Pos.Application.Features.Sales.QuotePosCart;

/// <summary>
/// A priced cart: what checkout would charge, line by line and in total, and whether the terminal's selling location
/// seems to hold enough unreserved stock (a hint; checkout checks again under the warehouse lock).
/// </summary>
/// <param name="StockShort">True when any line seems short of stock.</param>
public sealed record PosCartQuoteResponse(
    string CurrencyCode, int CurrencyDecimals, decimal MaxLineDiscountPercent, IReadOnlyList<PosCartQuoteLineResponse> Lines,
    decimal GrossAmount, decimal DiscountAmount, decimal NetAmount, decimal TaxAmount, decimal TotalAmount, bool StockShort);
