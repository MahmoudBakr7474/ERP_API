using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.ProductTaxes;

/// <summary>A product's tax classification, which a till prices it with.</summary>
/// <param name="TaxRate">A standard-rated product's own rate; null takes the sales settings' default VAT rate.</param>
public sealed record ProductTaxResponse(
    Guid Id, Guid ProductId, string Sku, string ProductName, SalesTaxTreatment TaxTreatment, decimal? TaxRate,
    DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
