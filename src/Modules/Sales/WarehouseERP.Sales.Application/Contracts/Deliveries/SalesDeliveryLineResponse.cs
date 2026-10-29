namespace WarehouseERP.Sales.Application.Contracts.Deliveries;

/// <param name="StockLineId">The stock document line Inventory posted, once posted.</param>
/// <param name="CostAmount">What the goods left stock at (the cost of goods sold), once posted.</param>
/// <param name="ReturnedQuantity">What approved customer returns brought back from this line.</param>
public sealed record SalesDeliveryLineResponse(
    Guid Id, int LineNumber, Guid OrderLineId, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId, string UnitCode,
    decimal ConversionFactor, decimal Quantity, decimal BaseQuantity, Guid LocationId, string LocationCode, Guid? StockLineId,
    decimal? CostAmount, decimal ReturnedQuantity);
