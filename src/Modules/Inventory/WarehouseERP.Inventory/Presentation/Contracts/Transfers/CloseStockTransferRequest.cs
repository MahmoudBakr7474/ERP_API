using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Presentation.Contracts.Transfers;

public sealed record CloseStockTransferRequest(AdjustmentReason Reason, string? Notes, string RowVersion);
