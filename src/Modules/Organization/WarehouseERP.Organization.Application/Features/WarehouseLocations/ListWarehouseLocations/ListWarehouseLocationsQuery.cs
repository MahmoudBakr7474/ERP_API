using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.ListWarehouseLocations;

/// <param name="Search">Matches the code or either name.</param>
public sealed record ListWarehouseLocationsQuery(Guid WarehouseId, int Page, int? PageSize, string? Search, bool? IsActive)
    : IPagedQuery;
