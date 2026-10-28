using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>What a priced line says about its price, as entered.</summary>
/// <param name="UnitPrice">Price of one unit of the line's product unit, before discount and tax.</param>
/// <param name="DiscountPercent">Line discount, 0 to 100.</param>
/// <param name="TaxRate">Tax rate in percent, 0 to 100, charged on the discounted amount.</param>
/// <param name="TaxTreatment">Whether the tax is recoverable; only recoverable tax is supported.</param>
public sealed record LinePrice(decimal UnitPrice, decimal DiscountPercent, decimal TaxRate, TaxTreatment TaxTreatment);
