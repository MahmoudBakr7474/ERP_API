namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.BankReconciliations;

/// <summary>A posted line of a bank's ledger account: money in positive, out negative.</summary>
public sealed record BankLine(
    Guid JournalLineId, Guid JournalId, string JournalNumber, DateOnly PostingDate, string Description, string? Reference, decimal Amount);
