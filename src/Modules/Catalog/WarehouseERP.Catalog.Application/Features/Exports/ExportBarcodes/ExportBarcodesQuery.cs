using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportBarcodes;

/// <summary>The selected products' barcodes with the unit each one is printed on.</summary>
/// <param name="MaxRows">Set by the server from the format's limit (Documents options), never by the client.</param>
public sealed record ExportBarcodesQuery(string? Search, Guid? CategoryId, ProductType? Type, bool? ProductIsActive, BarcodeType? BarcodeType, bool? IsActive, int MaxRows);
