using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Organization.Application.Features.ListWarehouses;

public sealed record ListWarehousesQuery(int Page, int? PageSize, Guid? BranchId) : IPagedQuery;
