namespace WarehouseERP.Sales.Contracts;

/// <summary>
/// Sales' price list and amount rules for a module that sells without its own (POS). A line takes the list price of its
/// product unit (tax-exclusive, base currency), its product's tax classification, a line discount in percent, and amounts
/// rounded line by line to the currency's minor unit exactly as sales documents are: gross, discount, net, tax, total.
/// Nothing here is manual: a unit without a list price or a product without a tax classification is refused.
/// </summary>
public interface ISalesPricing
{
    /// <summary>
    /// The active list price of each of these product units and the tax classification of its product, for a till to
    /// show when an item is scanned; missing prices or classifications come back as nulls, never as an error.
    /// </summary>
    Task<IReadOnlyList<SalesUnitPrice>> FindUnitPricesAsync(IReadOnlyCollection<SalesProductUnit> units, CancellationToken ct);

    /// <summary>
    /// Prices every line, or refuses with Sales' error code for the first line that can't be priced. With no lines it only
    /// tells the currency amounts are in and its decimals.
    /// </summary>
    Task<SalesPricingOutcome> PriceLinesAsync(IReadOnlyList<SalesPricingLine> lines, CancellationToken ct);
}
