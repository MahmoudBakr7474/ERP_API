using WarehouseERP.Accounting.Application.Contracts.SourcePostings;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceSyncStatus;

/// <summary>Where the automatic journaling of operational documents stands.</summary>
/// <param name="WorkerEnabled">Whether the background worker runs; otherwise documents are journaled on demand only.</param>
/// <param name="WorkerIntervalSeconds">How often the worker runs.</param>
/// <param name="OldestUnsettledAtUtc">When the oldest document still pending or failed posted in its module.</param>
/// <param name="Cursors">How far each module's documents have been looked for.</param>
public sealed record SourceSyncStatusResponse(
    bool IsActivated, DateTime? ActivatedAtUtc, DateOnly CutoverDate, bool WorkerEnabled, int WorkerIntervalSeconds, int Pending,
    int Failed, int Posted, int NothingToPost, DateTime? OldestUnsettledAtUtc, IReadOnlyList<SourceCursorResponse> Cursors);
