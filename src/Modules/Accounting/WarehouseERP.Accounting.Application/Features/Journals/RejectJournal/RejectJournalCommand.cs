namespace WarehouseERP.Accounting.Application.Features.Journals.RejectJournal;

/// <param name="Reason">Why it goes back to draft; kept in its history.</param>
public sealed record RejectJournalCommand(Guid JournalId, string Reason, string RowVersion);
