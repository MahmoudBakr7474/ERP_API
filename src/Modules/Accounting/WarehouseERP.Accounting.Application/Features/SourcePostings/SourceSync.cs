using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;
using WarehouseERP.Accounting.Application.Contracts.SourcePostings;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings;

// One run, by the worker or on demand, once accounting is active:
// 1. discovery (one transaction, under the scan lock): each module lists what it posted since a little before its last
//    scan (from the cutover's start the first time, or on a full scan); every document stamped from the cutover's start
//    (kept at activation) that has no accounting identity yet gets one (unique by type and id, so overlaps and concurrent
//    runs never count it twice), dated on its company-local date (never before the start date);
// 2. posting: pending documents and failures whose retry is due, oldest first, each in its own transaction (SourcePoster),
//    up to the batch size.
// Documents posted before the cutover's start never get an identity: the opening balances hold them.
public sealed partial class SourceSync(
    ITransactionRunner transactions, ISourcePostingRepository postings, IAccountingSettingsRepository settings,
    AccountingReferences references, IAccountingLocks locks, SourceDocuments documents, SourcePoster poster,
    IOptions<AccountingIntegrationOptions> options, TimeProvider clock, ILogger<SourceSync> logger)
{
    public async Task<Result<SourceSyncResponse>> RunAsync(bool fullScan, CancellationToken ct)
    {
        var policies = await settings.GetAsync(ct);
        if (!policies.IsActivated)
            return SourcePostingErrors.NotActive;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var zone = BusinessDates.ZoneOf(company.Value);
        var cutoverStart = policies.CutoverStartUtc
            ?? throw new InvalidOperationException("Accounting is active but its cutover start was not kept.");
        var discovered = await transactions.RunAsync(token => DiscoverAsync(policies.CutoverDate, cutoverStart, zone, fullScan, token), ct);
        if (discovered.IsFailure)
            return discovered.Error;

        var batch = options.Value.BatchSize;
        var due = await postings.ListDueAsync(clock.GetUtcNow().UtcDateTime, batch, ct);
        int posted = 0, nothing = 0, failed = 0;
        foreach (var id in due)
        {
            switch (await poster.PostAsync(id, ct))
            {
                case SourcePostingStatus.Posted:
                    posted++;
                    break;
                case SourcePostingStatus.NothingToPost:
                    nothing++;
                    break;
                case SourcePostingStatus.Failed:
                    failed++;
                    break;
            }
        }
        if (discovered.Value + due.Count > 0)
            RunFinished(logger, discovered.Value, posted, nothing, failed);
        return new SourceSyncResponse(discovered.Value, posted, nothing, failed, due.Count == batch);
    }

    private async Task<Result<int>> DiscoverAsync(DateOnly cutover, DateTime cutoverStart, TimeZoneInfo zone, bool fullScan, CancellationToken ct)
    {
        if (!await locks.TryLockSourceScanAsync(ct))
            return 0; // another run is scanning; whatever it misses the next run finds
        var now = clock.GetUtcNow().UtcDateTime;
        var found = 0;
        foreach (var module in SourceDocuments.Modules)
        {
            var cursor = await postings.GetCursorAsync(module, ct);
            var from = fullScan || cursor is null
                ? cutoverStart
                : Max(cutoverStart, cursor.ScannedToUtc - options.Value.ScanOverlap);
            foreach (var group in (await documents.ListAsync(module, from, now, ct)).GroupBy(d => d.Type))
            {
                var known = await postings.ExistingAsync(group.Key, group.Select(d => d.Id).ToList(), ct);
                foreach (var document in group.Where(d => !known.Contains(d.Id) && d.OccurredAtUtc >= cutoverStart))
                {
                    var local = BusinessDates.LocalDate(document.OccurredAtUtc, zone);
                    var date = local < cutover ? cutover : local;
                    postings.Add(SourcePosting.Discover(document.Type, document.Id, document.Number, document.OccurredAtUtc, date, now));
                    found++;
                }
            }
            if (cursor is null)
                postings.AddCursor(SourceCursor.Start(module, now, now));
            else
                cursor.Advance(now, now);
        }
        var saved = await postings.SaveChangesAsync(ct);
        return saved.IsFailure ? saved.Error : found;
    }

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    [LoggerMessage(EventId = 9210, Level = LogLevel.Information,
        Message = "Accounting run: {Discovered} document(s) found, {Posted} journaled, {Nothing} with nothing to post, {Failed} failed")]
    private static partial void RunFinished(ILogger logger, int discovered, int posted, int nothing, int failed);
}
