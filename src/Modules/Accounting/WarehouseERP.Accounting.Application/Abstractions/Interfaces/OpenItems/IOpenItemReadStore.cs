using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;

/// <summary>Read side of open items.</summary>
public interface IOpenItemReadStore
{
    /// <summary>A page of items, oldest due first, untracked.</summary>
    Task<PagedResult<OpenItem>> ListAsync(OpenItemFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>The active allocations that settle any of these items, untracked.</summary>
    Task<IReadOnlyList<Allocation>> AllocationsOfAsync(IReadOnlyCollection<Guid> openItemIds, CancellationToken ct);
}
