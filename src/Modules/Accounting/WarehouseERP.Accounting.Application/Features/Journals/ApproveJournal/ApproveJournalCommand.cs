namespace WarehouseERP.Accounting.Application.Features.Journals.ApproveJournal;

/// <summary>Approves a submitted journal, which posts it; its writer may approve it.</summary>
/// <param name="IdempotencyKey">A new GUID per approval click, sent again unchanged when the client retries.</param>
public sealed record ApproveJournalCommand(Guid JournalId, string RowVersion, Guid IdempotencyKey);
