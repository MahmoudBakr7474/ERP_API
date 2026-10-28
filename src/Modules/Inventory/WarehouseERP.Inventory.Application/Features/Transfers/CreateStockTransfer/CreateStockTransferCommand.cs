using WarehouseERP.Inventory.Application.Contracts.Transfers;

namespace WarehouseERP.Inventory.Application.Features.Transfers.CreateStockTransfer;

/// <param name="TransitWarehouseId">The transit warehouse; the source branch's only active one when left out.</param>
public sealed record CreateStockTransferCommand(
    Guid SourceWarehouseId, Guid DestinationWarehouseId, Guid? TransitWarehouseId, string? ExternalReference, string? Notes,
    IReadOnlyList<TransferLineRequest> Lines);
