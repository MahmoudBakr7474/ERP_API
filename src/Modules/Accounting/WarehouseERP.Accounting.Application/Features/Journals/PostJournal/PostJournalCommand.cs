namespace WarehouseERP.Accounting.Application.Features.Journals.PostJournal;

/// <summary>Posts a draft that needs no approval.</summary>
/// <param name="IdempotencyKey">A new GUID per posting click, sent again unchanged when the client retries.</param>
public sealed record PostJournalCommand(Guid JournalId, string RowVersion, Guid IdempotencyKey);
