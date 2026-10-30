using WarehouseERP.Accounting.Application.Contracts.Journals;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;

/// <summary>Read side of journals.</summary>
public interface IJournalReadStore
{
    /// <summary>The journal with its lines, history and reversal links, or null.</summary>
    Task<JournalResponse?> GetAsync(Guid journalId, CancellationToken ct);

    /// <summary>A page of journals, newest posting date first.</summary>
    Task<PagedResult<JournalSummaryResponse>> ListAsync(JournalFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>What posting answered for a posted journal (an idempotent retry gets it again), or null.</summary>
    Task<JournalPostedResponse?> FindPostedAsync(Guid journalId, CancellationToken ct);
}
