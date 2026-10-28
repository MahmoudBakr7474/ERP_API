namespace WarehouseERP.Inventory.Application.Features.Transfers.SubmitStockTransfer;

public sealed record SubmitStockTransferCommand(Guid TransferId, string RowVersion);
