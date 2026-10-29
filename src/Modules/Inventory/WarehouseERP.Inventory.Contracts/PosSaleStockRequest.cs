namespace WarehouseERP.Inventory.Contracts;

/// <summary>A POS sale to take out of stock.</summary>
/// <param name="SourceDocumentId">The POS sale; one stock document per id.</param>
/// <param name="SourceNumber">Its receipt number, kept as the stock document's external reference.</param>
/// <param name="WarehouseId">The terminal's physical warehouse, which must allow selling.</param>
/// <param name="Notes">Notes copied onto the stock document.</param>
/// <param name="Lines">The sold lines, in the sale's order; the stock document numbers them the same way.</param>
public sealed record PosSaleStockRequest(
    Guid SourceDocumentId, string SourceNumber, Guid WarehouseId, string? Notes, IReadOnlyList<PosSaleStockLine> Lines);
