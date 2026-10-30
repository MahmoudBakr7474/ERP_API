namespace WarehouseERP.Accounting.Application.Features.Journals.ReverseJournal;

/// <summary>Drafts the reversal of a posted manual or opening-balance journal; it posts like a manual journal.</summary>
/// <param name="PostingDate">On or after the reversed journal's date (and the accounting start date).</param>
/// <param name="Description">Defaults to "عكس القيد" and the reversed journal's number.</param>
/// <param name="Reference">Defaults to the reversed journal's number.</param>
public sealed record ReverseJournalCommand(Guid JournalId, DateOnly PostingDate, string? Description, string? Reference);
