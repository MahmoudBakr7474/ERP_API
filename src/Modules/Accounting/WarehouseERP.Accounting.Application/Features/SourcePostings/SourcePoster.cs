using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings;

// Journals one operational document as one transaction: its lock, its record read again (a document already journaled is
// left alone), its posting rule, every role resolved to its mapped account (an unmapped role stops it), the lines checked
// like any journal's, its parties' and cashboxes' locks and the cash guard, the period of its date locked shared and found
// open, its number; then the posted journal, its parties' open items and the record marked posted commit together. A
// failure rolls all of that back and is recorded on the record in a second transaction, with when it is tried again: the
// document is never lost, and nothing waits on it in its own module. A document another run holds is left to that run.
public sealed partial class SourcePoster(
    ITransactionRunner transactions, ISourcePostingRepository postings, SourceEntryBuilder entries, IAccountMappingRepository mappings,
    JournalLineRules lineRules, PostingGuard guard, LedgerPosting posting, IJournalRepository journals, IOpenItemRepository openItems,
    AccountingReferences references, IAccountingLocks locks, TimeProvider clock, ILogger<SourcePoster> logger)
{
    public async Task<SourcePostingStatus> PostAsync(Guid sourcePostingId, CancellationToken ct)
    {
        Result<SourcePostingStatus> result;
        try
        {
            result = await transactions.RunAsync(token => PostOnceAsync(sourcePostingId, token), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            PostingCrashed(logger, ex, sourcePostingId);
            result = SourcePostingErrors.Unexpected(ex.GetType().Name);
        }
        if (result.IsSuccess)
            return result.Value;
        if (result.Error.Code == SourcePostingErrors.Busy.Code)
            return SourcePostingStatus.Pending; // the run holding it (or that journaled it at the same moment) settles it
        await transactions.RunAsync(token => MarkFailedAsync(sourcePostingId, result.Error, token), ct);
        return SourcePostingStatus.Failed;
    }

    private async Task<Result<SourcePostingStatus>> PostOnceAsync(Guid sourcePostingId, CancellationToken ct)
    {
        if (!await locks.TryLockSourceAsync(sourcePostingId, ct))
            return SourcePostingErrors.Busy;
        var record = await postings.GetForUpdateAsync(sourcePostingId, ct);
        if (record is null)
            return SourcePostingErrors.NotFound;
        if (record.IsSettled)
            return record.Status;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var currency = company.Value.BaseCurrencyCode;
        var entry = await entries.BuildAsync(record, Money.CurrencyDecimals(currency), ct);
        if (entry.IsFailure)
            return entry.Error;

        var accounts = (await mappings.ListAsync(ct)).ToDictionary(m => m.Role, m => m.AccountId);
        var lines = new List<JournalLineInput>();
        foreach (var line in entry.Value.Lines.Where(l => l.Amount != 0m))
        {
            Guid accountId;
            if (line.AccountId is { } named)
                accountId = named;
            else if (!accounts.TryGetValue(line.Role!.Value, out accountId))
                return MappingErrors.RoleNotMapped(line.Role.Value);
            lines.Add(new JournalLineInput(accountId, entry.Value.BranchId, Math.Max(line.Amount, 0m), Math.Max(-line.Amount, 0m), null,
                line.PartyType, line.PartyId, line.DueDate));
        }
        var now = clock.GetUtcNow().UtcDateTime;
        if (lines.Count == 0)
        {
            record.MarkNothingToPost(entry.Value.StockValue, now);
            var savedEmpty = await postings.SaveChangesAsync(ct);
            return savedEmpty.IsFailure ? savedEmpty.Error : SourcePostingStatus.NothingToPost;
        }
        var checkedLines = await lineRules.CheckAsync(lines, entry.Value.JournalType, ct);
        if (checkedLines.IsFailure)
            return checkedLines.Error;
        var created = JournalEntry.CreateFromSource(entry.Value.JournalType, record.SourceType, record.SourceId, record.PostingDate,
            entry.Value.Description, entry.Value.Reference, currency, lines, now);
        if (created.IsFailure)
            return created.Error;
        var journal = created.Value;
        var guarded = await guard.LockAndCheckAsync(journal, ct);
        if (guarded.IsFailure)
            return guarded.Error;
        var slot = await posting.ReserveAsync(journal.JournalType, journal.PostingDate, intoClosedPeriod: false, ct);
        if (slot.IsFailure)
            return slot.Error;

        journal.MarkPostedFromSource(slot.Value.Number, slot.Value.FiscalPeriodId, now);
        journals.Add(journal);
        foreach (var line in journal.Lines.Where(l => l.HasParty))
            openItems.Add(OpenItem.Open(journal, line, now));
        record.MarkPosted(journal.Id, slot.Value.Number.Number, entry.Value.StockValue, entry.Value.PostedStockValue, entry.Value.VarianceAmount, now);
        var saved = await journals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        SourcePosted(logger, record.SourceType, record.SourceNumber, slot.Value.Number.Number, journal.TotalDebit);
        return SourcePostingStatus.Posted;
    }

    private async Task<Result<Done>> MarkFailedAsync(Guid sourcePostingId, Error error, CancellationToken ct)
    {
        if (!await locks.TryLockSourceAsync(sourcePostingId, ct))
            return Done.Value; // another run holds it now and records its own outcome
        var record = await postings.GetForUpdateAsync(sourcePostingId, ct);
        if (record is null || record.IsSettled)
            return Done.Value;
        record.MarkFailed(error.Code, error.Message, clock.GetUtcNow().UtcDateTime);
        var saved = await postings.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        SourceFailed(logger, record.SourceType, record.SourceNumber, error.Code, record.Attempts);
        return Done.Value;
    }

    [LoggerMessage(EventId = 9200, Level = LogLevel.Information, Message = "{SourceType} {SourceNumber} journaled as {Number} for {Total}")]
    private static partial void SourcePosted(ILogger logger, SourceDocumentType sourceType, string sourceNumber, string number, decimal total);

    [LoggerMessage(EventId = 9201, Level = LogLevel.Warning, Message = "{SourceType} {SourceNumber} could not be journaled ({ErrorCode}), attempt {Attempts}")]
    private static partial void SourceFailed(ILogger logger, SourceDocumentType sourceType, string sourceNumber, string errorCode, int attempts);

    [LoggerMessage(EventId = 9202, Level = LogLevel.Error, Message = "Journaling source posting {SourcePostingId} failed unexpectedly")]
    private static partial void PostingCrashed(ILogger logger, Exception exception, Guid sourcePostingId);
}
