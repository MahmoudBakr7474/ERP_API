using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Reports.ListUnitConversions;

/// <param name="Search">Matches the product's SKU or either name.</param>
/// <param name="ProductIsActive">The product's status; each row also shows its unit's own status.</param>
public sealed record ListUnitConversionsQuery(
    int Page, int? PageSize, string? Search = null, Guid? CategoryId = null, ProductType? Type = null,
    bool? ProductIsActive = null, Guid? UnitId = null) : IPagedQuery;
