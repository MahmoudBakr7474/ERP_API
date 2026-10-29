namespace WarehouseERP.Inventory.Contracts;

/// <summary>A POS customer return to put back into stock.</summary>
/// <param name="SourceDocumentId">The POS return; one stock document per id.</param>
/// <param name="SourceNumber">Its number, kept as the stock document's external reference.</param>
/// <param name="WarehouseId">The physical warehouse the goods were sold from and come back into.</param>
/// <param name="Notes">Notes copied onto the stock document.</param>
/// <param name="Lines">The returned lines, in the return's order.</param>
public sealed record PosReturnStockRequest(
    Guid SourceDocumentId, string SourceNumber, Guid WarehouseId, string? Notes, IReadOnlyList<PosReturnStockLine> Lines);
