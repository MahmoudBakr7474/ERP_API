using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Application.Contracts.Branches;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;

/// <summary>Read side of branches: projections, no tracking.</summary>
public interface IBranchReadStore
{
    Task<BranchDetailsResponse?> GetAsync(Guid branchId, CancellationToken ct);

    Task<PagedResult<BranchSummaryResponse>> ListAsync(BranchListFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>True when one of these warehouses is in the branch: how a user without company-wide access reaches it.</summary>
    Task<bool> HoldsAnyWarehouseAsync(Guid branchId, IReadOnlySet<Guid> warehouseIds, CancellationToken ct);
}
