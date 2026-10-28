using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Features.Transfers.CloseStockTransfer;

/// <param name="Reason">Why the outstanding quantity never arrived (loss, damage, …).</param>
public sealed record CloseStockTransferCommand(Guid TransferId, AdjustmentReason Reason, string? Notes, string RowVersion);
