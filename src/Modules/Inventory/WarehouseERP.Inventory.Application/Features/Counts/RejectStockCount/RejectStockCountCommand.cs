namespace WarehouseERP.Inventory.Application.Features.Counts.RejectStockCount;

/// <param name="Reason">Why it goes back for counting; kept in the count's history.</param>
public sealed record RejectStockCountCommand(Guid CountId, string Reason, string RowVersion);
