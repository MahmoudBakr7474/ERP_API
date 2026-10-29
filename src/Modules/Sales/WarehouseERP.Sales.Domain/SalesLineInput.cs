using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>A quotation or order line as agreed, with its product unit's facts from Catalog and where its price came from.</summary>
/// <param name="Quantity">Quantity in the product unit.</param>
/// <param name="ListUnitPrice">The price list's price for the product unit when the line was entered; null when it had none.</param>
public sealed record SalesLineInput(
    ProductUnitFacts Unit, decimal Quantity, LinePrice Price, PriceSource PriceSource, decimal? ListUnitPrice);
