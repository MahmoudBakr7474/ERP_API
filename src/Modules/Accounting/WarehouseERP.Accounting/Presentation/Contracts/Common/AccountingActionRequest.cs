namespace WarehouseERP.Accounting.Presentation.Contracts.Common;

/// <summary>An action on an accounting record from the version the client last read.</summary>
public sealed record AccountingActionRequest(string RowVersion);
