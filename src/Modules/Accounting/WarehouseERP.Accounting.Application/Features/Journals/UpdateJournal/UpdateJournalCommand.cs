using WarehouseERP.Accounting.Application.Contracts.Journals;

namespace WarehouseERP.Accounting.Application.Features.Journals.UpdateJournal;

/// <summary>Replaces a draft's date, texts and lines.</summary>
/// <param name="PostingDate">Ignored for opening balances, which always take the accounting start date.</param>
/// <param name="Lines">Null keeps the current lines; a reversal's lines can't be sent.</param>
public sealed record UpdateJournalCommand(
    Guid JournalId, DateOnly PostingDate, string Description, string? Reference, IReadOnlyList<JournalLineRequest>? Lines, string RowVersion);
