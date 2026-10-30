using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Contracts.Journals;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals;

// Posting a journal (any hand-made type: manual, opening, reversal, voucher), directly or by approval, as one transaction:
// 1. the key is locked and looked up (a retry gets the first answer); the journal is loaded at the version the client read;
//    its date and lines are checked again;
// 2. its parties' locks, then its cashboxes' and banks' locks, in id order; a cashbox (or a bank without overdraft) it
//    would take below zero at the end of its date or of any later day refuses it (PostingGuard);
// 3. a reversal needs the reversed journal's open items unallocated; a voucher's planned allocations are loaded;
// 4. the period of its date is locked shared and found open, and the journal takes its number;
// 5. every control-account line opens its party's item; a reversal's items settle the reversed journal's items, a
//    voucher's item settles the items it names.
// The journal, its number, items, allocations and the idempotency record commit together or not at all.
public sealed partial class JournalPosting(
    ITransactionRunner transactions, IdempotentRequests requests, IJournalRepository journals, IJournalReadStore readStore,
    IAccountingSettingsRepository settings, JournalLineRules lineRules, LedgerPosting posting, IOpenItemRepository openItems,
    PostingGuard guard, ICurrentUser currentUser, TimeProvider clock, ILogger<JournalPosting> logger)
{
    public Task<Result<JournalPostedResponse>> PostAsync(Guid journalId, byte[] rowVersion, Guid idempotencyKey, bool byApproval, CancellationToken ct) =>
        transactions.RunAsync(token => PostOnceAsync(journalId, rowVersion, idempotencyKey, byApproval, token), ct);

    private async Task<Result<JournalPostedResponse>> PostOnceAsync(
        Guid journalId, byte[] rowVersion, Guid idempotencyKey, bool byApproval, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var operation = byApproval ? "ApproveJournal" : "PostJournal";
        var fingerprint = RequestFingerprint.Of(operation, journalId, rowVersion, userId);
        var previous = await requests.BeginAsync(idempotencyKey, fingerprint, ct);
        if (previous.IsFailure)
            return previous.Error;
        if (previous.Value is { } postedId)
            return await readStore.FindPostedAsync(postedId, ct)
                ?? throw new InvalidOperationException($"Idempotency record points at journal {postedId}, which is not posted.");

        var journal = await journals.GetForUpdateAsync(journalId, expectedRowVersion: null, ct);
        if (journal is null)
            return JournalErrors.NotFound;
        if (!journal.RowVersion.AsSpan().SequenceEqual(rowVersion))
            return CommonErrors.ConcurrencyConflict;
        var allowed = journal.CanPost(byApproval);
        if (allowed.IsFailure)
            return allowed.Error;
        var policies = await settings.GetAsync(ct);
        if (policies.IsActivated && (journal.JournalType == JournalType.OpeningBalance || await ReversesOpeningAsync(journal, ct)))
            return JournalErrors.OpeningAfterActivation;
        if (journal.JournalType == JournalType.OpeningBalance && journal.PostingDate != policies.CutoverDate)
            return JournalErrors.OpeningDateMismatch(policies.CutoverDate);
        if (journal.PostingDate < policies.CutoverDate)
            return JournalErrors.BeforeCutover(policies.CutoverDate);
        var lines = await lineRules.CheckAsync(journal.Lines.OrderBy(l => l.LineNumber).Select(l => l.ToInput()).ToList(), journal.JournalType, ct);
        if (lines.IsFailure)
            return lines.Error;

        var guarded = await guard.LockAndCheckAsync(journal, ct);
        if (guarded.IsFailure)
            return guarded.Error;
        if (journal.ReversalOfJournalId is { } reversedId && await openItems.HasActiveAllocationsAsync(reversedId, ct))
            return JournalErrors.ItemsAllocated;
        var targets = await openItems.GetForUpdateAsync(journal.Allocations.Select(a => a.OpenItemId).ToList(), ct);
        for (var i = 0; i < journal.Allocations.Count; i++)
            if (targets.All(t => t.Id != journal.Allocations[i].OpenItemId))
                return VoucherErrors.AllocationItemNotFound(i + 1);

        var slot = await posting.ReserveAsync(journal.JournalType, journal.PostingDate, intoClosedPeriod: false, ct);
        if (slot.IsFailure)
            return slot.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        journal.MarkPosted(slot.Value.Number, slot.Value.FiscalPeriodId, byApproval, userId, now);
        var opened = new Dictionary<int, OpenItem>();
        foreach (var line in journal.Lines.Where(l => l.HasParty))
        {
            var item = OpenItem.Open(journal, line, now);
            openItems.Add(item);
            opened[line.LineNumber] = item;
        }
        var settled = journal.ReversalOfJournalId is { } originalId
            ? await SettleReversedAsync(journal, originalId, opened, userId, now, ct)
            : SettlePlanned(journal, opened, targets, userId, now);
        if (settled.IsFailure)
            return settled.Error;
        requests.Complete(idempotencyKey, operation, fingerprint, journal.Id, userId, now);
        var saved = await journals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        JournalPosted(logger, journal.Id, slot.Value.Number.Number, journal.TotalDebit);
        return new JournalPostedResponse(journal.Id, slot.Value.Number.Number, journal.JournalType, journal.PostingDate, journal.TotalDebit, now);
    }

    private async Task<bool> ReversesOpeningAsync(JournalEntry journal, CancellationToken ct) =>
        journal.ReversalOfJournalId is { } originalId
        && (await journals.FindAsync(originalId, ct))?.JournalType == JournalType.OpeningBalance;

    // A reversal's lines mirror the original's, line by line, so each new item settles the item the same line opened.
    private async Task<Result> SettleReversedAsync(
        JournalEntry reversal, Guid originalId, Dictionary<int, OpenItem> opened, Guid? userId, DateTime now, CancellationToken ct)
    {
        var original = await journals.GetForUpdateAsync(originalId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Reversal {reversal.Id} names journal {originalId}, which does not exist.");
        original.RecordReversed(reversal.Number!, userId, now);
        var lineNumbers = original.Lines.ToDictionary(l => l.Id, l => l.LineNumber);
        foreach (var item in await openItems.ListOfJournalForUpdateAsync(originalId, ct))
        {
            var mirror = opened[lineNumbers[item.JournalLineId]];
            var allocation = item.IsDebit
                ? Allocation.Create(item, mirror, item.Unsettled, reversal.PostingDate, reversal.Id, userId, now)
                : Allocation.Create(mirror, item, item.Unsettled, reversal.PostingDate, reversal.Id, userId, now);
            if (allocation.IsFailure)
                return allocation.Error;
            openItems.Add(allocation.Value);
        }
        return Result.Success();
    }

    // A receipt, payment or refund settles the open items its draft named, from its own item (its one party line). Other
    // journals, an opening balance with many party lines among them, plan nothing.
    private Result SettlePlanned(
        JournalEntry journal, Dictionary<int, OpenItem> opened, IReadOnlyList<OpenItem> targets, Guid? userId, DateTime now)
    {
        if (journal.Allocations.Count == 0)
            return Result.Success();
        if (opened.Count != 1)
            throw new InvalidOperationException($"Voucher {journal.Id} plans allocations but opened {opened.Count} items of its own.");
        var own = opened.Values.Single();
        for (var i = 0; i < journal.Allocations.Count; i++)
        {
            var planned = journal.Allocations[i];
            var target = targets.Single(t => t.Id == planned.OpenItemId);
            if (target.IsDebit == own.IsDebit)
                return VoucherErrors.AllocationItemMismatch(i + 1);
            var settlesOn = target.DocumentDate > journal.PostingDate ? target.DocumentDate : journal.PostingDate;
            var allocation = own.IsDebit
                ? Allocation.Create(own, target, planned.Amount, settlesOn, journal.Id, userId, now)
                : Allocation.Create(target, own, planned.Amount, settlesOn, journal.Id, userId, now);
            if (allocation.IsFailure)
                return allocation.Error == AllocationDomainErrors.ExceedsOpen ? VoucherErrors.AllocationExceedsOpen(i + 1)
                    : allocation.Error == AllocationDomainErrors.AmountInvalid ? allocation.Error : VoucherErrors.AllocationItemMismatch(i + 1);
            openItems.Add(allocation.Value);
        }
        return Result.Success();
    }

    [LoggerMessage(EventId = 9040, Level = LogLevel.Information, Message = "Journal {JournalId} posted as {Number} for {Total}")]
    private static partial void JournalPosted(ILogger logger, Guid journalId, string number, decimal total);
}
