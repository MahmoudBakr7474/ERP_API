using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;

/// <summary>One posted journal line with its journal's number, type, date and texts.</summary>
public sealed record LedgerLine(
    Guid JournalId, string JournalNumber, JournalType JournalType, DateOnly PostingDate, string JournalDescription, string? Reference,
    int LineNumber, Guid AccountId, Guid BranchId, decimal Debit, decimal Credit, string? Description);
