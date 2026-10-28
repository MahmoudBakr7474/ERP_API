namespace WarehouseERP.Inventory.Contracts;

/// <summary>A receipt or return Purchasing posts to stock.</summary>
/// <param name="SourceDocumentId">The purchasing document (goods receipt or supplier return); one stock document per id.</param>
/// <param name="SourceNumber">Its number, kept as the stock document's external reference.</param>
/// <param name="WarehouseId">The physical warehouse the goods come into or leave.</param>
/// <param name="Notes">Notes copied onto the stock document.</param>
/// <param name="ApprovedByUserId">Who approved the purchasing document, when it needed approval.</param>
/// <param name="Lines">The lines, in the purchasing document's order; the stock document numbers them the same way.</param>
public sealed record PurchaseStockRequest(
    Guid SourceDocumentId, string SourceNumber, Guid WarehouseId, string? Notes, Guid? ApprovedByUserId,
    IReadOnlyList<PurchaseStockLine> Lines);
