namespace WarehouseERP.Sales.Contracts;

/// <summary>A priced line: the list price, discount and tax it took, and its rounded amounts in the base currency.</summary>
/// <param name="UnitPrice">The list price of one unit, tax-exclusive.</param>
/// <param name="DiscountPercent">The line discount.</param>
/// <param name="TaxTreatment">The product's tax classification.</param>
/// <param name="TaxRate">VAT in percent: above 0 for standard-rated lines, 0 otherwise.</param>
/// <param name="GrossAmount">Quantity times unit price, rounded.</param>
/// <param name="DiscountAmount">Gross times the discount, rounded.</param>
/// <param name="NetAmount">Gross less discount.</param>
/// <param name="TaxAmount">Net times the tax rate, rounded.</param>
/// <param name="TotalAmount">Net plus tax.</param>
/// <param name="NetUnitPrice">Unit price after discount, 8 decimals.</param>
public sealed record SalesPricedLine(
    decimal UnitPrice, decimal DiscountPercent, SalesTaxTreatment TaxTreatment, decimal TaxRate, decimal GrossAmount,
    decimal DiscountAmount, decimal NetAmount, decimal TaxAmount, decimal TotalAmount, decimal NetUnitPrice);
