using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Domain;

/// <summary>A document line as entered, with the product unit's facts the application looked up in Catalog.</summary>
/// <param name="ProductId">The product (a stock item).</param>
/// <param name="ProductUnitId">The unit the quantity is entered in.</param>
/// <param name="UnitId">That product unit's unit of measure.</param>
/// <param name="ConversionFactor">Base units in one entered unit.</param>
/// <param name="UnitAllowsFraction">Whether the entered unit may hold decimals.</param>
/// <param name="BaseUnitAllowsFraction">Whether the base unit may hold decimals.</param>
/// <param name="Quantity">Quantity in the entered unit.</param>
/// <param name="Direction">In or out.</param>
/// <param name="UnitCost">Cost of one entered unit in the base currency, for stock coming in.</param>
/// <param name="LocationId">Where the stock goes or comes from, in the document's warehouse.</param>
public sealed record StockLineInput(
    Guid ProductId, Guid ProductUnitId, Guid UnitId, decimal ConversionFactor, bool UnitAllowsFraction,
    bool BaseUnitAllowsFraction, decimal Quantity, StockDirection Direction, decimal? UnitCost, Guid LocationId);
