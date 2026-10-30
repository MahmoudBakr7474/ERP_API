using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetPartyStatement;

/// <param name="Balance">The running balance after this line, debit minus credit.</param>
public sealed record PartyStatementLineResponse(
    DateOnly PostingDate, Guid JournalId, string JournalNumber, JournalType JournalType, string Description, string? Reference, decimal Debit,
    decimal Credit, decimal Balance);
