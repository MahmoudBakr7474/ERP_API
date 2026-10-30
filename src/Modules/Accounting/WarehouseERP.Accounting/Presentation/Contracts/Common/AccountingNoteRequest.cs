namespace WarehouseERP.Accounting.Presentation.Contracts.Common;

/// <summary>An action that only needs a reason (reopening a period).</summary>
public sealed record AccountingNoteRequest(string? Reason);
