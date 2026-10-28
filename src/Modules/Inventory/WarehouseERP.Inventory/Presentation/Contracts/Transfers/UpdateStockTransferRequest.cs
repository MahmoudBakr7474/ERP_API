using WarehouseERP.Inventory.Application.Contracts.Transfers;

namespace WarehouseERP.Inventory.Presentation.Contracts.Transfers;

/// <summary>The draft transfer's new transit warehouse (null keeps it), references and lines; the id comes from the route.</summary>
public sealed record UpdateStockTransferRequest(
    Guid? TransitWarehouseId, string? ExternalReference, string? Notes, IReadOnlyList<TransferLineRequest> Lines, string RowVersion);
