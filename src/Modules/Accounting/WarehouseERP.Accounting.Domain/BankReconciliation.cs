using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// A bank statement matched against the bank account's ledger: the posted lines up to the statement date are ticked as
/// cleared, and the reconciliation completes only when the balance cleared so far plus the ticked lines equals the
/// statement's closing balance (differences such as bank charges are posted first). A line is cleared by one
/// reconciliation only. Practical V1: entered by hand, no statement import.
/// </summary>
public sealed class BankReconciliation : IAuditable
{
    private readonly List<BankReconciliationLine> _lines = [];

    private BankReconciliation() { } // EF Core

    public Guid Id { get; private set; }
    public Guid CashAccountId { get; private set; }
    public DateOnly StatementDate { get; private set; }

    /// <summary>The statement's closing balance (money in the bank: positive).</summary>
    public decimal StatementBalance { get; private set; }

    public BankReconciliationStatus Status { get; private set; }

    /// <summary>What earlier completed reconciliations had cleared, kept when this one completes.</summary>
    public decimal? OpeningClearedBalance { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? CompletedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyList<BankReconciliationLine> Lines => _lines;

    public decimal ClearedAmount => _lines.Sum(l => l.Amount);

    public static BankReconciliation Start(Guid cashAccountId, DateOnly statementDate, decimal statementBalance) => new()
    {
        CashAccountId = cashAccountId,
        StatementDate = statementDate,
        StatementBalance = statementBalance,
        Status = BankReconciliationStatus.Draft,
    };

    /// <summary>The ticked lines, replacing the earlier selection; the caller checked they are the bank's and still free.</summary>
    public Result Tick(IReadOnlyList<(Guid JournalLineId, decimal Amount)> lines)
    {
        if (Status != BankReconciliationStatus.Draft)
            return BankReconciliationDomainErrors.NotDraft;
        _lines.Clear();
        _lines.AddRange(lines.Select(l => BankReconciliationLine.Create(l.JournalLineId, l.Amount)));
        return Result.Success();
    }

    /// <summary>The statement balance minus what is cleared once this reconciliation completes.</summary>
    public decimal Difference(decimal openingClearedBalance) => StatementBalance - (openingClearedBalance + ClearedAmount);

    public Result Complete(decimal openingClearedBalance, Guid? userId, DateTime nowUtc)
    {
        if (Status != BankReconciliationStatus.Draft)
            return BankReconciliationDomainErrors.NotDraft;
        var difference = Difference(openingClearedBalance);
        if (difference != 0m)
            return BankReconciliationDomainErrors.NotBalanced(difference);
        Status = BankReconciliationStatus.Completed;
        OpeningClearedBalance = openingClearedBalance;
        CompletedAtUtc = nowUtc;
        CompletedByUserId = userId;
        return Result.Success();
    }

    /// <summary>Drops a draft and frees its lines.</summary>
    public Result Cancel()
    {
        if (Status != BankReconciliationStatus.Draft)
            return BankReconciliationDomainErrors.NotDraft;
        _lines.Clear();
        Status = BankReconciliationStatus.Cancelled;
        return Result.Success();
    }
}
