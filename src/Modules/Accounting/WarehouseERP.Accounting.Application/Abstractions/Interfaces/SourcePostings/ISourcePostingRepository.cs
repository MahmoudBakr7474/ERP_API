using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;

/// <summary>Write side of operational documents' accounting identities and the scan cursors; over the shared ErpDbContext.</summary>
public interface ISourcePostingRepository
{
    /// <summary>Tracked, or null.</summary>
    Task<SourcePosting?> GetForUpdateAsync(Guid sourcePostingId, CancellationToken ct);

    /// <summary>Which of these documents of a type already have their accounting identity.</summary>
    Task<IReadOnlySet<Guid>> ExistingAsync(WarehouseERP.Accounting.Contracts.SourceDocumentType sourceType, IReadOnlyCollection<Guid> sourceIds, CancellationToken ct);

    /// <summary>Pending documents and failed ones whose retry is due, oldest document first, at most <paramref name="max"/>.</summary>
    Task<IReadOnlyList<Guid>> ListDueAsync(DateTime nowUtc, int max, CancellationToken ct);

    /// <summary>Tracked, or null before the module's first scan.</summary>
    Task<SourceCursor?> GetCursorAsync(string module, CancellationToken ct);

    void Add(SourcePosting posting);
    void AddCursor(SourceCursor cursor);

    /// <summary>The save point; a document found twice at the same moment comes back as a conflict.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
