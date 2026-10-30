using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Contracts.Imports;

/// <summary>What a committed import did, per sheet and in total.</summary>
public sealed record CatalogImportResultResponse(
    CatalogImportMode Mode, CatalogImportCounts Totals, IReadOnlyList<CatalogImportSheetSummary> Sheets);
