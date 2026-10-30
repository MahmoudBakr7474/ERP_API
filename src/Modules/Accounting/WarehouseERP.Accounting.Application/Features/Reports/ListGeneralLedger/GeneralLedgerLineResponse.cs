using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Reports.ListGeneralLedger;

/// <param name="Description">The line's own text, or its journal's description.</param>
public sealed record GeneralLedgerLineResponse(
    DateOnly PostingDate, Guid JournalId, string JournalNumber, JournalType JournalType, int LineNumber, Guid AccountId, string AccountCode,
    string AccountNameAr, Guid BranchId, string? BranchCode, string Description, string? Reference, decimal Debit, decimal Credit);
