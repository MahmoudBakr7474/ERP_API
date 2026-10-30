using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Contracts.Imports;

public sealed record CatalogImportSheetSummary(CatalogImportSheet Sheet, CatalogImportCounts Counts);
