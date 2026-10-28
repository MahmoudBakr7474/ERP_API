namespace WarehouseERP.Inventory.Application.Features.Transfers.ApproveStockTransfer;

/// <param name="IdempotencyKey">Chosen by the client for this click and sent again on a retry.</param>
/// <param name="RowVersion">The version the approver saw; a changed transfer is not dispatched (409).</param>
public sealed record ApproveStockTransferCommand(Guid TransferId, Guid IdempotencyKey, string RowVersion);
