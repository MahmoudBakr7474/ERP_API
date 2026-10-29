using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Documents;

/// <summary>One quotation or order line as the client sends it. Prices are in the company's base currency, before tax.</summary>
/// <param name="ProductId">An active stock item without batch or serial tracking.</param>
/// <param name="ProductUnitId">An active unit of the product; the price list prices each unit.</param>
/// <param name="Quantity">Up to 4 decimals; whole for a unit that can't be split.</param>
/// <param name="UnitPrice">
/// Leave it out to take the price list's price. Any other price (or a price where the list has none) is a manual price:
/// only users with sales.prices.manage may set it.
/// </param>
/// <param name="DiscountPercent">0 to 100, up to 4 decimals.</param>
/// <param name="TaxTreatment">Standard when left out.</param>
/// <param name="TaxRate">For a standard-rated line, the company's default VAT rate when left out; zero-rated and exempt lines take none.</param>
public sealed record SalesLineRequest(
    Guid ProductId, Guid ProductUnitId, decimal Quantity, decimal? UnitPrice = null, decimal DiscountPercent = 0,
    SalesTaxTreatment? TaxTreatment = null, decimal? TaxRate = null);
