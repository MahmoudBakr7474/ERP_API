namespace WarehouseERP.Inventory.Contracts;

/// <summary>A delivery Sales posts to stock.</summary>
/// <param name="SourceDocumentId">The sales delivery; one stock document per id.</param>
/// <param name="SourceNumber">Its number, kept as the stock document's external reference.</param>
/// <param name="WarehouseId">The order's physical warehouse.</param>
/// <param name="ReservationSourceId">The sales order whose reservations the lines consume.</param>
/// <param name="Notes">Notes copied onto the stock document.</param>
/// <param name="Lines">The picked lines, in the delivery's order; several may consume one order line.</param>
public sealed record SalesDeliveryStockRequest(
    Guid SourceDocumentId, string SourceNumber, Guid WarehouseId, Guid ReservationSourceId, string? Notes,
    IReadOnlyList<SalesDeliveryStockLine> Lines);
