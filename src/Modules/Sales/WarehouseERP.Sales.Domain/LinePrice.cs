using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>What a sales line says about its price, as agreed.</summary>
/// <param name="UnitPrice">Price of one unit of the line's product unit, before discount and tax (tax-exclusive).</param>
/// <param name="DiscountPercent">Line discount, 0 to 100.</param>
/// <param name="TaxTreatment">Standard, zero-rated or exempt.</param>
/// <param name="TaxRate">VAT in percent on the discounted amount: above 0 for standard-rated lines, 0 otherwise.</param>
public sealed record LinePrice(decimal UnitPrice, decimal DiscountPercent, SalesTaxTreatment TaxTreatment, decimal TaxRate);
