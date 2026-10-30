using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Journals;

/// <summary>A journal with its lines, history and reversal links.</summary>
/// <param name="DraftReference">DJ-000123, kept after posting.</param>
/// <param name="FiscalPeriodNumber">The period it posted into (with its year's name); null until posted.</param>
/// <param name="ReversedByJournalId">Its reversal (drafted, submitted or posted), if any.</param>
public sealed record JournalResponse(
    Guid Id, string DraftReference, string? Number, JournalType JournalType, JournalStatus Status, DateOnly PostingDate,
    string? FiscalYearName, int? FiscalPeriodNumber, string Description, string? Reference, string CurrencyCode, bool RequiresApproval,
    bool LinesFixed, decimal TotalDebit, decimal TotalCredit, Guid? ReversalOfJournalId, string? ReversalOfNumber,
    Guid? ReversedByJournalId, string? ReversedByNumber, JournalStatus? ReversedByStatus, Guid? CreatedByUserId, string? CreatedByName,
    DateTime CreatedAtUtc, Guid? ApprovedByUserId, string? ApprovedByName, Guid? PostedByUserId, string? PostedByName,
    DateTime? PostedAtUtc, IReadOnlyList<JournalLineResponse> Lines, IReadOnlyList<JournalEventResponse> Events, string RowVersion);
