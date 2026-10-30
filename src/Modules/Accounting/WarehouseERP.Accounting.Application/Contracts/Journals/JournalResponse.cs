using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Journals;

/// <summary>A journal with its lines, history and reversal links.</summary>
/// <param name="DraftReference">DJ-000123, kept after posting.</param>
/// <param name="FiscalPeriodNumber">The period it posted into (with its year's name); null until posted.</param>
/// <param name="ReversedByJournalId">Its reversal (drafted, submitted or posted), if any.</param>
/// <param name="Voucher">A voucher's cash account, party, method, amounts and planned allocations; null for other journals.</param>
/// <param name="SourceType">For a system journal of an operational document (IV, PU, SL, PS): the document's type; null otherwise.</param>
/// <param name="SourceId">That document's id in its module.</param>
public sealed record JournalResponse(
    Guid Id, string DraftReference, string? Number, JournalType JournalType, JournalStatus Status, DateOnly PostingDate,
    string? FiscalYearName, int? FiscalPeriodNumber, string Description, string? Reference, string CurrencyCode, bool RequiresApproval,
    bool LinesFixed, decimal TotalDebit, decimal TotalCredit, Guid? ReversalOfJournalId, string? ReversalOfNumber,
    Guid? ReversedByJournalId, string? ReversedByNumber, JournalStatus? ReversedByStatus, Guid? CreatedByUserId, string? CreatedByName,
    DateTime CreatedAtUtc, Guid? ApprovedByUserId, string? ApprovedByName, Guid? PostedByUserId, string? PostedByName,
    DateTime? PostedAtUtc, VoucherResponse? Voucher, IReadOnlyList<JournalLineResponse> Lines, IReadOnlyList<JournalEventResponse> Events,
    string RowVersion, SourceDocumentType? SourceType, Guid? SourceId);
