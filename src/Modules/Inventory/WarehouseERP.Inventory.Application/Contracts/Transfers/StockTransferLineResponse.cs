namespace WarehouseERP.Inventory.Application.Contracts.Transfers;

/// <param name="Quantity">In the entered unit.</param>
/// <param name="BaseQuantity">Dispatched (or to dispatch), in the base unit.</param>
/// <param name="ReceivedQuantity">Arrived so far, base unit.</param>
/// <param name="LostQuantity">Written off by the loss adjustment that closed the transfer, base unit.</param>
/// <param name="OutstandingQuantity">Still in transit, base unit.</param>
public sealed record StockTransferLineResponse(
    Guid Id, int LineNumber, Guid ProductId, string Sku, string ProductNameAr, Guid ProductUnitId, string UnitCode,
    decimal ConversionFactor, decimal Quantity, decimal BaseQuantity, string BaseUnitCode, Guid SourceLocationId,
    string SourceLocationCode, decimal ReceivedQuantity, decimal LostQuantity, decimal OutstandingQuantity);
