using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Contracts.Reports;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;

/// <summary>
/// The catalog's report data, read without tracking. Screens and downloads use the same methods (an export asks for
/// one page, one row larger than its limit), so a report and its PDF, XLSX and CSV always hold the same rows.
/// </summary>
public interface ICatalogReportReadStore
{
    Task<CatalogSummaryResponse> GetSummaryAsync(CancellationToken ct);

    /// <summary>The product list's filter and order (<see cref="ProductListFilter"/>), with names resolved.</summary>
    Task<PagedResult<ProductMasterResponse>> ListProductsAsync(ProductListFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>Product units ordered by SKU, then factor.</summary>
    Task<PagedResult<UnitConversionResponse>> ListUnitConversionsAsync(
        UnitConversionFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>Barcodes ordered by SKU, unit factor, then value.</summary>
    Task<PagedResult<BarcodeReportResponse>> ListBarcodesAsync(BarcodeReportFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>Every category in tree order with its product counts, then the "no category" row.</summary>
    Task<IReadOnlyList<CategoryProductCountResponse>> ListCategoryProductCountsAsync(bool? categoryIsActive, CancellationToken ct);

    Task<IReadOnlyList<UnitUsageResponse>> ListUnitUsageAsync(bool? isActive, CancellationToken ct);

    /// <summary>Every issue of the selected products, ordered by SKU, then issue.</summary>
    Task<IReadOnlyList<DataQualityIssueResponse>> ListDataQualityIssuesAsync(DataQualityFilter filter, CancellationToken ct);

    Task<ProductSheetResponse?> GetProductSheetAsync(Guid productId, CancellationToken ct);
}
