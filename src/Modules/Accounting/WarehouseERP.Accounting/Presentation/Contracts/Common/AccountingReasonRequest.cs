namespace WarehouseERP.Accounting.Presentation.Contracts.Common;

/// <summary>An action that needs a reason (reject, reopen), from the version the client last read.</summary>
public sealed record AccountingReasonRequest(string? Reason, string RowVersion);
