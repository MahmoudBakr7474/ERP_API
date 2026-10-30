using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Journals;

public sealed record JournalSummaryResponse(
    Guid Id, string DraftReference, string? Number, JournalType JournalType, JournalStatus Status, DateOnly PostingDate,
    string Description, string? Reference, decimal TotalDebit, int LineCount, Guid? ReversalOfJournalId, DateTime CreatedAtUtc,
    DateTime? PostedAtUtc);
