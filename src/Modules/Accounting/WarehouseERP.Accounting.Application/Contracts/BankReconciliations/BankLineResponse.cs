namespace WarehouseERP.Accounting.Application.Contracts.BankReconciliations;

/// <summary>A posted line of the bank's ledger account (money in positive, out negative) and whether this reconciliation ticked it.</summary>
public sealed record BankLineResponse(
    Guid JournalLineId, Guid JournalId, string JournalNumber, DateOnly PostingDate, string Description, string? Reference, decimal Amount,
    bool IsTicked);
