namespace WarehouseERP.Inventory.Presentation.Contracts.Counts;

/// <summary>Start, submit, approve or cancel: the row version the user saw.</summary>
public sealed record StockCountActionRequest(string RowVersion);
