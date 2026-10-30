using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportProducts;

/// <summary>The product master list with the product list's filters and order.</summary>
/// <param name="MaxRows">Set by the server from the format's limit (Documents options), never by the client.</param>
public sealed record ExportProductsQuery(string? Search, Guid? CategoryId, ProductType? Type, bool? IsActive, ProductSort? Sort, bool Descending, int MaxRows);
