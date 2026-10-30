namespace WarehouseERP.Inventory.Contracts;

/// <summary>
/// What a posted stock document did to the company's stock value, exactly (4 decimals, base currency).
/// AdjustmentReason: An adjustment's reason; for a reversal, the reversed adjustment's. ReversedDocumentType: For a
/// reversal, the type of the document it reverses. ReversedNumber: For a reversal, the number of the document it
/// reverses. ValueIn: Sum of the value its incoming lines added. ValueOut: Sum of the value its outgoing lines took
/// away (positive).
/// </summary>
public sealed record InventoryDocumentValue(
    Guid DocumentId, StockDocumentType DocumentType, string Number, Guid BranchId, DateTime PostedAtUtc,
    AdjustmentReason? AdjustmentReason, StockDocumentType? ReversedDocumentType, string? ReversedNumber, decimal ValueIn,
    decimal ValueOut);
