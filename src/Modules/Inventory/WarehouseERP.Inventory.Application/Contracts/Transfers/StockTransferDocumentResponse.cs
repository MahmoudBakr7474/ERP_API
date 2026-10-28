using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.Transfers;

/// <summary>A ledger document a transfer produced: its dispatch, a receipt, or the loss adjustment that closes it.</summary>
public sealed record StockTransferDocumentResponse(
    Guid Id, StockDocumentType DocumentType, StockDocumentStatus Status, string DraftReference, string? Number,
    DateTime CreatedAtUtc, DateTime? PostedAtUtc);
