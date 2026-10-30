using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// A customer's or supplier's open item: one per posted line on a control account, so the subledger always adds up to the
/// ledger. Its amount is signed like the line (debit minus credit: an invoice or a refund owed to us is positive, a
/// receipt, credit or bill is negative); allocations bring its open amount towards zero, never past it.
/// </summary>
public sealed class OpenItem
{
    private OpenItem() { } // EF Core

    public Guid Id { get; private set; }
    public PartyType PartyType { get; private set; }
    public Guid PartyId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public Guid JournalLineId { get; private set; }
    public JournalType JournalType { get; private set; }

    /// <summary>The journal's number (CR-2027-000001, OB-2027-000001…).</summary>
    public string DocumentNumber { get; private set; } = null!;

    /// <summary>The journal's outside reference (an invoice, bill or cheque number).</summary>
    public string? Reference { get; private set; }

    public DateOnly DocumentDate { get; private set; }
    public DateOnly DueDate { get; private set; }

    /// <summary>Debit minus credit of its line.</summary>
    public decimal Amount { get; private set; }

    /// <summary>What is still unsettled, with the amount's sign; zero once fully allocated.</summary>
    public decimal OpenAmount { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public bool IsDebit => Amount > 0m;

    public bool IsSettled => OpenAmount == 0m;

    /// <summary>The item a posted control-account line opens.</summary>
    public static OpenItem Open(JournalEntry journal, JournalLine line, DateTime nowUtc)
    {
        if (!journal.IsPosted || journal.Number is null)
            throw new InvalidOperationException($"Journal {journal.Id} is not posted.");
        if (line.PartyType is not { } partyType || line.PartyId is not { } partyId)
            throw new InvalidOperationException($"Line {line.Id} names no party.");
        var amount = line.Debit - line.Credit;
        return new OpenItem
        {
            PartyType = partyType,
            PartyId = partyId,
            AccountId = line.AccountId,
            BranchId = line.BranchId,
            JournalEntryId = journal.Id,
            JournalLineId = line.Id,
            JournalType = journal.JournalType,
            DocumentNumber = journal.Number,
            Reference = journal.Reference,
            DocumentDate = journal.PostingDate,
            DueDate = line.DueDate ?? journal.PostingDate,
            Amount = amount,
            OpenAmount = amount,
            CreatedAtUtc = nowUtc,
        };
    }

    /// <summary>How much of it is still open, as a positive amount.</summary>
    public decimal Unsettled => Math.Abs(OpenAmount);

    internal void Settle(decimal amount)
    {
        if (amount <= 0m || amount > Unsettled)
            throw new InvalidOperationException($"Open item {Id} can't settle {amount}; {Unsettled} is open.");
        OpenAmount = IsDebit ? OpenAmount - amount : OpenAmount + amount;
    }

    internal void Restore(decimal amount)
    {
        if (amount <= 0m || amount > Math.Abs(Amount) - Unsettled)
            throw new InvalidOperationException($"Open item {Id} can't take back {amount}.");
        OpenAmount = IsDebit ? OpenAmount + amount : OpenAmount - amount;
    }
}
