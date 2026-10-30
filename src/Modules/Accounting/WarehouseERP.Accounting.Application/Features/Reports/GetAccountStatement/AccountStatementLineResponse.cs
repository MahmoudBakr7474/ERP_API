using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetAccountStatement;

/// <param name="Description">The line's own text, or its journal's description.</param>
/// <param name="Balance">The running balance after this line, debit minus credit.</param>
public sealed record AccountStatementLineResponse(
    DateOnly PostingDate, Guid JournalId, string JournalNumber, JournalType JournalType, string Description, string? Reference,
    Guid AccountId, string AccountCode, Guid BranchId, string? BranchCode, decimal Debit, decimal Credit, decimal Balance);
