namespace WarehouseERP.Accounting.Domain;

/// <summary>A posted line of the bank's ledger account ticked as cleared on the statement, with its amount (debit minus credit).</summary>
public sealed class BankReconciliationLine
{
    private BankReconciliationLine() { } // EF Core

    public long Id { get; private set; }
    public Guid BankReconciliationId { get; private set; }
    public Guid JournalLineId { get; private set; }
    public decimal Amount { get; private set; }

    internal static BankReconciliationLine Create(Guid journalLineId, decimal amount) => new() { JournalLineId = journalLineId, Amount = amount };
}
