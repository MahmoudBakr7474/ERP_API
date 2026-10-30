using WarehouseERP.Accounting.Application.Contracts.SourcePostings;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;

/// <summary>Read side of operational documents' accounting: the exception queue, its counts and the reconciliation sums.</summary>
public interface ISourcePostingReadStore
{
    /// <summary>Newest document first.</summary>
    Task<PagedResult<SourcePostingResponse>> ListAsync(SourcePostingFilter filter, int page, int pageSize, CancellationToken ct);

    Task<SourcePostingResponse?> FindAsync(Guid sourcePostingId, CancellationToken ct);

    /// <summary>How many documents of each type stand in each status.</summary>
    Task<IReadOnlyList<SourceStatusCount>> CountsAsync(CancellationToken ct);

    Task<IReadOnlyList<SourceCursorResponse>> CursorsAsync(CancellationToken ct);

    /// <summary>How many documents dated in these dates are still pending or failed.</summary>
    Task<int> CountUnsettledAsync(DateOnly from, DateOnly to, CancellationToken ct);

    /// <summary>When the oldest document still pending or failed posted in its module; null when none is.</summary>
    Task<DateTime?> OldestUnsettledAsync(CancellationToken ct);

    /// <summary>Every document that has its accounting identity, by type and id.</summary>
    Task<IReadOnlySet<(WarehouseERP.Accounting.Contracts.SourceDocumentType Type, Guid Id)>> RegisteredAsync(CancellationToken ct);

    /// <summary>The stock values of documents journaled (or with nothing to post): exact and posted.</summary>
    Task<SourceStockTotals> StockTotalsAsync(CancellationToken ct);

    /// <summary>
    /// Journals naming an operational document that its accounting identity does not point back to, plus identities marked
    /// posted whose journal does not name them: both should be none.
    /// </summary>
    Task<int> UnmatchedJournalsAsync(CancellationToken ct);
}
