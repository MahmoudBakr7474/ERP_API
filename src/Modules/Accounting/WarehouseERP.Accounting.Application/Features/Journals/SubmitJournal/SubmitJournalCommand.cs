namespace WarehouseERP.Accounting.Application.Features.Journals.SubmitJournal;

public sealed record SubmitJournalCommand(Guid JournalId, string RowVersion);
