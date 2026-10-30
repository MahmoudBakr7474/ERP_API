using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportProductUnits;

/// <summary>Every unit the selected products are handled in, with its conversion factor.</summary>
/// <param name="MaxRows">Set by the server from the format's limit (Documents options), never by the client.</param>
public sealed record ExportProductUnitsQuery(string? Search, Guid? CategoryId, ProductType? Type, bool? ProductIsActive, Guid? UnitId, int MaxRows);
