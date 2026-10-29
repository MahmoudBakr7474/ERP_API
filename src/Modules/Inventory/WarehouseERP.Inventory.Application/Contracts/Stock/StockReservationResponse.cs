namespace WarehouseERP.Inventory.Application.Contracts.Stock;

/// <summary>Stock a sales order line holds in a warehouse, in base units.</summary>
/// <param name="SourceDocumentId">The sales order.</param>
/// <param name="SourceLineId">The sales order line.</param>
/// <param name="SourceNumber">The order's number.</param>
/// <param name="RemainingQuantity">Reserved minus consumed by deliveries minus released.</param>
public sealed record StockReservationResponse(
    Guid Id, Guid SourceDocumentId, Guid SourceLineId, string SourceNumber, Guid ProductId, string Sku, Guid WarehouseId,
    string WarehouseCode, decimal ReservedQuantity, decimal ConsumedQuantity, decimal ReleasedQuantity, decimal RemainingQuantity,
    DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
