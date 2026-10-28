using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.CreateStockDocument;

/// <param name="AdjustmentReason">Adjustments only, and required for them.</param>
public sealed record CreateStockDocumentCommand(
    StockDocumentType DocumentType, Guid WarehouseId, AdjustmentReason? AdjustmentReason, string? ExternalReference,
    string? Notes, IReadOnlyList<StockLineRequest> Lines);
