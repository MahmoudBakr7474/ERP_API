namespace WarehouseERP.Inventory.Application.Features.Transfers.ReceiveStockTransfer;

/// <param name="LineId">The transfer line that arrived.</param>
/// <param name="Quantity">How much of it, in the product's base unit; at most what is still in transit.</param>
/// <param name="LocationId">Where it goes in the destination warehouse; the DEFAULT location when left out.</param>
public sealed record TransferReceiptLineRequest(Guid LineId, decimal Quantity, Guid? LocationId = null);
