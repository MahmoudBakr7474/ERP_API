using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Contracts.Imports;

/// <summary>What confirming the import does with one row; <paramref name="Key"/> is branch/warehouse/location code.</summary>
public sealed record LocationImportRowResult(int Row, string? Key, LocationImportAction Action);
