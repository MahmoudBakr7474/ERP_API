namespace WarehouseERP.Catalog.Application.Contracts.Reports;

/// <summary>
/// Catalog counts for the dashboard and the reports page. The last three concern active products and use the same
/// definitions as the data-quality report.
/// </summary>
public sealed record CatalogSummaryResponse(
    int Products, int ActiveProducts, int InactiveProducts, int StockItems, int NonStockItems, int Services,
    int Categories, int ActiveCategories, int Units, int ActiveUnits, int ActiveBarcodes,
    int ProductsWithoutBarcode, int ProductsWithoutCategory, int ProductsWithUnitIssues);
