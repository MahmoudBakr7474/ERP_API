using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;

/// <summary>Customers' and suppliers' open items and their allocations; changed only under the party's lock.</summary>
public interface IOpenItemRepository
{
    /// <summary>The items among these ids, untracked (to learn their party before taking its lock).</summary>
    Task<IReadOnlyList<OpenItem>> FindAsync(IReadOnlyCollection<Guid> openItemIds, CancellationToken ct);

    /// <summary>The items among these ids, tracked.</summary>
    Task<IReadOnlyList<OpenItem>> GetForUpdateAsync(IReadOnlyCollection<Guid> openItemIds, CancellationToken ct);

    /// <summary>The items a posted journal opened, tracked.</summary>
    Task<IReadOnlyList<OpenItem>> ListOfJournalForUpdateAsync(Guid journalId, CancellationToken ct);

    /// <summary>True when an active allocation settles an item the journal opened.</summary>
    Task<bool> HasActiveAllocationsAsync(Guid journalId, CancellationToken ct);

    /// <summary>The party's items still open, tracked, oldest due first.</summary>
    Task<IReadOnlyList<OpenItem>> ListOpenForUpdateAsync(PartyType partyType, Guid partyId, CancellationToken ct);

    /// <summary>The allocation, untracked (to learn its party before taking its lock), or null.</summary>
    Task<Allocation?> FindAllocationAsync(Guid allocationId, CancellationToken ct);

    /// <summary>The allocation, tracked, or null.</summary>
    Task<Allocation?> GetAllocationForUpdateAsync(Guid allocationId, CancellationToken ct);

    void Add(OpenItem item);

    void Add(Allocation allocation);

    Task<Result> SaveChangesAsync(CancellationToken ct);
}
