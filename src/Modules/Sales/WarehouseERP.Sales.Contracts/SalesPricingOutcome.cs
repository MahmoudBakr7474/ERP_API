namespace WarehouseERP.Sales.Contracts;

/// <summary>
/// What pricing did: every line priced in the company's base currency, or why Sales refused (its own error code, for
/// example <c>Sales.Line.PriceMissing</c> or <c>Sales.ProductTax.NotClassified</c>, which the caller passes on).
/// </summary>
/// <param name="CurrencyCode">The base currency; null when refused.</param>
/// <param name="CurrencyDecimals">Decimals of its minor unit, which amounts are rounded to; 0 when refused.</param>
/// <param name="Lines">One per request line, in its order; empty when refused.</param>
/// <param name="ErrorCode">Sales' error code when refused.</param>
/// <param name="ErrorMessage">Sales' message when refused.</param>
public sealed record SalesPricingOutcome(
    string? CurrencyCode, int CurrencyDecimals, IReadOnlyList<SalesPricedLine> Lines, string? ErrorCode, string? ErrorMessage)
{
    /// <summary>True when every line was priced.</summary>
    public bool Succeeded => ErrorCode is null;

    /// <summary>Every line priced.</summary>
    public static SalesPricingOutcome Priced(string currencyCode, int currencyDecimals, IReadOnlyList<SalesPricedLine> lines) =>
        new(currencyCode, currencyDecimals, lines, null, null);

    /// <summary>A refusal with Sales' error.</summary>
    public static SalesPricingOutcome Refused(string errorCode, string errorMessage) => new(null, 0, [], errorCode, errorMessage);
}
