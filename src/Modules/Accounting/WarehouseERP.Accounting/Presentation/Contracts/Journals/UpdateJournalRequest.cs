using WarehouseERP.Accounting.Application.Contracts.Journals;

namespace WarehouseERP.Accounting.Presentation.Contracts.Journals;

/// <param name="Lines">Null keeps the current lines (a reversal's lines can't be sent).</param>
public sealed record UpdateJournalRequest(
    DateOnly PostingDate, string Description, string? Reference, IReadOnlyList<JournalLineRequest>? Lines, string RowVersion);
