namespace WarehouseERP.Inventory.Application.Features.Transfers.RejectStockTransfer;

/// <param name="Reason">Why it goes back to draft; kept in the transfer's history.</param>
public sealed record RejectStockTransferCommand(Guid TransferId, string Reason, string RowVersion);
