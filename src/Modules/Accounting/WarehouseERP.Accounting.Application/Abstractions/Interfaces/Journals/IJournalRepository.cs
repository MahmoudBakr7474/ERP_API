using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;

/// <summary>Write side of journals; implemented over the shared ErpDbContext.</summary>
public interface IJournalRepository
{
    /// <summary>The journal with its lines, untracked, or null.</summary>
    Task<JournalEntry?> FindAsync(Guid journalId, CancellationToken ct);

    /// <summary>Tracked with its lines, or null; with a row version, the save fails with a conflict unless it still has it.</summary>
    Task<JournalEntry?> GetForUpdateAsync(Guid journalId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>True when a reversal of the journal is drafted, submitted or posted.</summary>
    Task<bool> HasActiveReversalAsync(Guid journalId, CancellationToken ct);

    Task<bool> AnyPostedAsync(CancellationToken ct);

    /// <summary>How many drafts and submitted journals are dated in these dates.</summary>
    Task<int> CountOpenAsync(DateOnly from, DateOnly to, CancellationToken ct);

    /// <summary>How many drafts and submitted journals of a type there are.</summary>
    Task<int> CountOpenOfTypeAsync(WarehouseERP.Accounting.Contracts.JournalType type, CancellationToken ct);

    void Add(JournalEntry journal);

    /// <summary>The save point; a second reversal of one journal comes back as a conflict.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
