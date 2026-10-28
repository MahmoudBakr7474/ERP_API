namespace WarehouseERP.Inventory.Application.Features.Transfers.ReceiveStockTransfer;

/// <param name="IdempotencyKey">Chosen by the client for this click and sent again on a retry.</param>
/// <param name="RowVersion">The version the user saw; a changed transfer receives nothing (409).</param>
public sealed record ReceiveStockTransferCommand(
    Guid TransferId, Guid IdempotencyKey, IReadOnlyList<TransferReceiptLineRequest> Lines, string RowVersion);
