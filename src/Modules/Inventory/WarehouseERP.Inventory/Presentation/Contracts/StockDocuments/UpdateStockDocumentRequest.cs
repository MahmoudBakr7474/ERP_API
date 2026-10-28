using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Presentation.Contracts.StockDocuments;

/// <summary>The draft's new reason, references and lines; the id comes from the route.</summary>
public sealed record UpdateStockDocumentRequest(
    AdjustmentReason? AdjustmentReason, string? ExternalReference, string? Notes, IReadOnlyList<StockLineRequest> Lines,
    string RowVersion);
