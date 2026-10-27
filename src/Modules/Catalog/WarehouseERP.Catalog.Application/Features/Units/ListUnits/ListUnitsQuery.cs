using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Catalog.Application.Features.Units.ListUnits;

/// <param name="Search">Matches the code or either name.</param>
public sealed record ListUnitsQuery(int Page, int? PageSize, string? Search, bool? IsActive) : IPagedQuery;
