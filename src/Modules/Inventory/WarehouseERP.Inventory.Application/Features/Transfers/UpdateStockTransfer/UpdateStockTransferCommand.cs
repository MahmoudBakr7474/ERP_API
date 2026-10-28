using WarehouseERP.Inventory.Application.Contracts.Transfers;

namespace WarehouseERP.Inventory.Application.Features.Transfers.UpdateStockTransfer;

/// <summary>Replaces a draft transfer's transit warehouse, references and lines; source and destination are fixed.</summary>
public sealed record UpdateStockTransferCommand(
    Guid TransferId, Guid? TransitWarehouseId, string? ExternalReference, string? Notes, IReadOnlyList<TransferLineRequest> Lines,
    string RowVersion);
