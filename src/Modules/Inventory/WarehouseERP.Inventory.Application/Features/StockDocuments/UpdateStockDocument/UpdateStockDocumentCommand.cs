using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.UpdateStockDocument;

/// <summary>Replaces a draft's reason, references and lines.</summary>
public sealed record UpdateStockDocumentCommand(
    Guid DocumentId, AdjustmentReason? AdjustmentReason, string? ExternalReference, string? Notes,
    IReadOnlyList<StockLineRequest> Lines, string RowVersion);
