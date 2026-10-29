using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Features.ProductTaxes.SetProductTax;

/// <summary>Classifies a product for VAT: standard-rated (at the default rate or its own), zero-rated or exempt.</summary>
/// <param name="TaxRate">Only for a standard-rated product with its own rate; null takes the default VAT rate.</param>
public sealed record SetProductTaxCommand(Guid ProductId, SalesTaxTreatment TaxTreatment, decimal? TaxRate);
