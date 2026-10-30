using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Contracts.Imports;

/// <summary>What a committed location import did.</summary>
public sealed record LocationImportResultResponse(LocationImportMode Mode, LocationImportCounts Totals);
