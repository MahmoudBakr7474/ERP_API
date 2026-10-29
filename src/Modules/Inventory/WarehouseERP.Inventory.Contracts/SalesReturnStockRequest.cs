namespace WarehouseERP.Inventory.Contracts;

/// <summary>A customer return Sales posts to stock.</summary>
/// <param name="SourceDocumentId">The sales return; one stock document per id.</param>
/// <param name="SourceNumber">Its number, kept as the stock document's external reference.</param>
/// <param name="WarehouseId">The physical warehouse the goods were delivered from and come back into.</param>
/// <param name="Notes">Notes copied onto the stock document.</param>
/// <param name="ApprovedByUserId">Who approved the return.</param>
/// <param name="Lines">The returned lines, in the return's order.</param>
public sealed record SalesReturnStockRequest(
    Guid SourceDocumentId, string SourceNumber, Guid WarehouseId, string? Notes, Guid? ApprovedByUserId,
    IReadOnlyList<SalesReturnStockLine> Lines);
