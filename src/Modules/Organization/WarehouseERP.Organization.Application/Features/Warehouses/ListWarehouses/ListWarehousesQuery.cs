using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Organization.Application.Features.Warehouses.ListWarehouses;

/// <param name="Search">Matches the code or either name.</param>
public sealed record ListWarehousesQuery(int Page, int? PageSize, Guid? BranchId, string? Search, bool? IsActive) : IPagedQuery;
