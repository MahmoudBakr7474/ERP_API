namespace WarehouseERP.Catalog.Application.Features.Exports.ExportUnits;

/// <summary>Units of measure with how many products use them.</summary>
/// <param name="MaxRows">Set by the server from the format's limit (Documents options), never by the client.</param>
public sealed record ExportUnitsQuery(bool? IsActive, int MaxRows);
