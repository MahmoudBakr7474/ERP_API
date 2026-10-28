using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.StockDocuments;

/// <summary>One line of a stock document as the client sends it.</summary>
/// <param name="ProductId">The product (a stock item without batch or serial tracking).</param>
/// <param name="ProductUnitId">The product unit the quantity is in: any active unit of the product.</param>
/// <param name="Quantity">Quantity in that unit, up to 4 decimals (whole for a unit that cannot be split).</param>
/// <param name="LocationId">A location of the document's warehouse; the DEFAULT location when left out.</param>
/// <param name="UnitCost">
/// Cost of one such unit in the base currency: required for opening stock and receipts, optional for an adjustment
/// increase (the average cost otherwise), never for stock going out.
/// </param>
/// <param name="Direction">In or out; adjustments only.</param>
public sealed record StockLineRequest(
    Guid ProductId, Guid ProductUnitId, decimal Quantity, Guid? LocationId = null, decimal? UnitCost = null,
    StockDirection? Direction = null);
