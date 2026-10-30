namespace WarehouseERP.Organization.Application.Features.Exports.ExportStructure;

/// <param name="IsActive">Only active (or only inactive) branches, warehouses and locations; null: all.</param>
/// <param name="IncludeLocations">List every location under its warehouse, not only the counts.</param>
/// <param name="MaxRows">Set by the server from the format's limit; branches, warehouses and locations all count.</param>
public sealed record ExportStructureQuery(bool? IsActive, bool IncludeLocations, int MaxRows);
