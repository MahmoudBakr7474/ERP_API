namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;

/// <param name="Search">Matches the product's SKU or either name.</param>
/// <param name="ProductIsActive">Which products are checked; the use cases default to active ones.</param>
public sealed record DataQualityFilter(string? Search, bool? ProductIsActive);
