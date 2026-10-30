using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Application.Contracts.Reports;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Reports;

/// <summary>
/// Read side of the organization reports: projections over the Organization tables, no tracking, no stored figures.
/// <c>scope</c> is the caller's warehouses (null: every warehouse); a scoped caller sees those warehouses, their
/// locations and the branches holding them.
/// </summary>
public interface IOrganizationReportReadStore
{
    Task<OrganizationSummaryResponse> GetSummaryAsync(IReadOnlySet<Guid>? scope, CancellationToken ct);

    /// <param name="isActive">Only active (or only inactive) branches, warehouses and counted locations; null: all.</param>
    Task<IReadOnlyList<StructureBranchResponse>> GetStructureAsync(bool? isActive, IReadOnlySet<Guid>? scope, CancellationToken ct);

    Task<PagedResult<LocationDirectoryResponse>> ListLocationsAsync(LocationDirectoryFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>Every configuration problem in scope, ordered by issue, branch, warehouse and code.</summary>
    Task<IReadOnlyList<OrganizationDataIssueResponse>> ListDataIssuesAsync(IReadOnlySet<Guid>? scope, CancellationToken ct);
}
