namespace WarehouseERP.Inventory.Application.Features.Transfers.CancelStockTransfer;

public sealed record CancelStockTransferCommand(Guid TransferId, string RowVersion);
