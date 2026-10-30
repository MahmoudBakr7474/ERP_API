using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// One debit open item settled against one credit open item of the same party and control account, by an amount neither
/// has left open: a receipt against an invoice, a bill against a payment, a credit against a debit. It moves nothing in the
/// ledger (both items are already posted), so the subledger keeps adding up to it. Removing it puts the amounts back; the
/// row stays as history.
/// </summary>
public sealed class Allocation
{
    private Allocation() { } // EF Core

    public Guid Id { get; private set; }
    public PartyType PartyType { get; private set; }
    public Guid PartyId { get; private set; }
    public Guid DebitItemId { get; private set; }
    public Guid CreditItemId { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>The business date it settles from (aging as of an earlier date ignores it).</summary>
    public DateOnly AllocatedOn { get; private set; }

    /// <summary>The voucher or reversal that made it; null when allocated by hand.</summary>
    public Guid? JournalEntryId { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? RemovedAtUtc { get; private set; }
    public Guid? RemovedByUserId { get; private set; }

    public static Result<Allocation> Create(
        OpenItem debit, OpenItem credit, decimal amount, DateOnly allocatedOn, Guid? journalEntryId, Guid? userId, DateTime nowUtc)
    {
        if (debit.PartyType != credit.PartyType || debit.PartyId != credit.PartyId)
            return AllocationDomainErrors.PartyMismatch;
        if (debit.AccountId != credit.AccountId)
            return AllocationDomainErrors.AccountMismatch;
        if (!debit.IsDebit || credit.IsDebit)
            return AllocationDomainErrors.SidesInvalid;
        if (amount <= 0m)
            return AllocationDomainErrors.AmountInvalid;
        if (amount > debit.Unsettled || amount > credit.Unsettled)
            return AllocationDomainErrors.ExceedsOpen;
        debit.Settle(amount);
        credit.Settle(amount);
        return new Allocation
        {
            PartyType = debit.PartyType,
            PartyId = debit.PartyId,
            DebitItemId = debit.Id,
            CreditItemId = credit.Id,
            Amount = amount,
            AllocatedOn = allocatedOn,
            JournalEntryId = journalEntryId,
            IsActive = true,
            CreatedAtUtc = nowUtc,
            CreatedByUserId = userId,
        };
    }

    public Result Remove(OpenItem debit, OpenItem credit, Guid? userId, DateTime nowUtc)
    {
        if (debit.Id != DebitItemId || credit.Id != CreditItemId)
            throw new InvalidOperationException($"Allocation {Id} settles other items.");
        if (!IsActive)
            return AllocationDomainErrors.AlreadyRemoved;
        debit.Restore(Amount);
        credit.Restore(Amount);
        IsActive = false;
        RemovedAtUtc = nowUtc;
        RemovedByUserId = userId;
        return Result.Success();
    }
}
