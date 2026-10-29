using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Presentation.Contracts.ProductTaxes;

/// <summary>The product's VAT treatment; a rate only for a standard-rated product with its own (null: the default VAT rate).</summary>
public sealed record SetProductTaxRequest(SalesTaxTreatment TaxTreatment, decimal? TaxRate);
