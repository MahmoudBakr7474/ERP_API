using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Organization.Application.Features.Warehouses.ListWarehouses;

public sealed record ListWarehousesQuery(int Page, int? PageSize, Guid? BranchId) : IPagedQuery;
