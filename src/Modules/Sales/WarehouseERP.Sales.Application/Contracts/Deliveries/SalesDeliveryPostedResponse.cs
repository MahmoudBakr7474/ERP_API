namespace WarehouseERP.Sales.Application.Contracts.Deliveries;

/// <summary>A posted delivery; a retry with the same Idempotency-Key gets the same answer.</summary>
/// <param name="StockDocumentNumber">The SDL-… stock document that took the goods out of stock.</param>
/// <param name="CostAmount">What the goods left stock at.</param>
public sealed record SalesDeliveryPostedResponse(
    Guid DeliveryId, string Number, Guid StockDocumentId, string StockDocumentNumber, decimal CostAmount, DateTime PostedAtUtc);
