namespace WarehouseERP.Inventory.Application.Features.Counts.ApproveStockCount;

/// <param name="IdempotencyKey">Chosen by the client for this click and sent again on a retry.</param>
/// <param name="RowVersion">The version the approver saw; a changed count is not approved (409).</param>
public sealed record ApproveStockCountCommand(Guid CountId, Guid IdempotencyKey, string RowVersion);
