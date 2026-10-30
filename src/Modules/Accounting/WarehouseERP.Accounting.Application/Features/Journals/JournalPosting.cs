using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
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

// Posting a journal, directly or by approval, as one transaction: the key is locked and looked up (a retry gets the first
// answer), the journal is loaded at the version the client read, its date and lines are checked again, then the period of
// its date is locked shared and found open and the journal takes its number. A reversal notes itself in the history of
// the journal it reverses. The journal, its number and the idempotency record commit together or not at all.
public sealed partial class JournalPosting(
    ITransactionRunner transactions, IdempotentRequests requests, IJournalRepository journals, IJournalReadStore readStore,
    IAccountingSettingsRepository settings, JournalLineRules lineRules, LedgerPosting posting, ICurrentUser currentUser,
    TimeProvider clock, ILogger<JournalPosting> logger)
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
        if (journal.JournalType == JournalType.OpeningBalance && journal.PostingDate != policies.CutoverDate)
            return JournalErrors.OpeningDateMismatch(policies.CutoverDate);
        if (journal.PostingDate < policies.CutoverDate)
            return JournalErrors.BeforeCutover(policies.CutoverDate);
        var lines = await lineRules.CheckAsync(journal.Lines.OrderBy(l => l.LineNumber).Select(l => l.ToInput()).ToList(), journal.JournalType, ct);
        if (lines.IsFailure)
            return lines.Error;
        var slot = await posting.ReserveAsync(journal.JournalType, journal.PostingDate, intoClosedPeriod: false, ct);
        if (slot.IsFailure)
            return slot.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        journal.MarkPosted(slot.Value.Number, slot.Value.FiscalPeriodId, byApproval, userId, now);
        if (journal.ReversalOfJournalId is { } reversedId)
        {
            var reversed = await journals.GetForUpdateAsync(reversedId, expectedRowVersion: null, ct)
                ?? throw new InvalidOperationException($"Reversal {journal.Id} names journal {reversedId}, which does not exist.");
            reversed.RecordReversed(slot.Value.Number.Number, userId, now);
        }
        requests.Complete(idempotencyKey, operation, fingerprint, journal.Id, userId, now);
        var saved = await journals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        JournalPosted(logger, journal.Id, slot.Value.Number.Number, journal.TotalDebit);
        return new JournalPostedResponse(journal.Id, slot.Value.Number.Number, journal.JournalType, journal.PostingDate, journal.TotalDebit, now);
    }

    [LoggerMessage(EventId = 9040, Level = LogLevel.Information, Message = "Journal {JournalId} posted as {Number} for {Total}")]
    private static partial void JournalPosted(ILogger logger, Guid journalId, string number, decimal total);
}
