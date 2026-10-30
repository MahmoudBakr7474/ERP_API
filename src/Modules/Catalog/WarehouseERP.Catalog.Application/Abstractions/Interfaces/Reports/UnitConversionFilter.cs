using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;

/// <param name="Search">Matches the product's SKU or either name.</param>
/// <param name="ProductIsActive">The product's status (each row also shows its unit's own status).</param>
public sealed record UnitConversionFilter(string? Search, Guid? CategoryId, ProductType? Type, bool? ProductIsActive, Guid? UnitId);
