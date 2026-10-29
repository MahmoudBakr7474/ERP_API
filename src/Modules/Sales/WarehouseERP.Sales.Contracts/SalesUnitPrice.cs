namespace WarehouseERP.Sales.Contracts;

/// <summary>A product unit's selling terms as a till shows them.</summary>
/// <param name="ProductId">The product.</param>
/// <param name="ProductUnitId">The product unit.</param>
/// <param name="UnitPrice">The active list price, tax-exclusive, in the base currency; null when the list has none.</param>
/// <param name="TaxTreatment">The product's tax classification; null when it has none (it can't be sold then).</param>
/// <param name="TaxRate">The VAT rate in percent a line would take: above 0 for standard-rated products, 0 otherwise.</param>
public sealed record SalesUnitPrice(
    Guid ProductId, Guid ProductUnitId, decimal? UnitPrice, SalesTaxTreatment? TaxTreatment, decimal TaxRate);
