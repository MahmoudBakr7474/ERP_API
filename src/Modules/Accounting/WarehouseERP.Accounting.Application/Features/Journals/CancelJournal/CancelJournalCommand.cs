namespace WarehouseERP.Accounting.Application.Features.Journals.CancelJournal;

public sealed record CancelJournalCommand(Guid JournalId, string RowVersion);
