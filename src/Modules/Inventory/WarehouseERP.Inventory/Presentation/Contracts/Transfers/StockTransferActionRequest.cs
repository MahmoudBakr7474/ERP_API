namespace WarehouseERP.Inventory.Presentation.Contracts.Transfers;

/// <summary>Submit, dispatch, approve or cancel: the row version the user saw.</summary>
public sealed record StockTransferActionRequest(string RowVersion);
