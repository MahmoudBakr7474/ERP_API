using WarehouseERP.Inventory.Application.Features.Transfers.ReceiveStockTransfer;

namespace WarehouseERP.Inventory.Presentation.Contracts.Transfers;

public sealed record ReceiveStockTransferRequest(IReadOnlyList<TransferReceiptLineRequest> Lines, string RowVersion);
