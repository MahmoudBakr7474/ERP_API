namespace WarehouseERP.Inventory.Contracts;

/// <summary>What a confirmed sales order reserves.</summary>
/// <param name="SourceDocumentId">The sales order.</param>
/// <param name="SourceNumber">Its number, shown with the reservation.</param>
/// <param name="WarehouseId">The physical warehouse, which must allow selling.</param>
/// <param name="Lines">One per order line; each line gets its own reservation.</param>
public sealed record StockReservationRequest(
    Guid SourceDocumentId, string SourceNumber, Guid WarehouseId, IReadOnlyList<StockReservationLine> Lines);
