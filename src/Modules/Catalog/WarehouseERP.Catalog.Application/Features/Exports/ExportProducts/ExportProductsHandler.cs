using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportProducts;

public sealed class ExportProductsHandler(ICatalogReportReadStore reports) : IQueryHandler<ExportProductsQuery, IReadOnlyList<ProductMasterResponse>>
{
    public async Task<Result<IReadOnlyList<ProductMasterResponse>>> HandleAsync(ExportProductsQuery query, CancellationToken ct)
    {
        var filter = new ProductListFilter(
            Text.Optional(query.Search), query.CategoryId, query.Type, query.IsActive, query.Sort ?? ProductSort.Sku, query.Descending);
        var rows = await reports.ListProductsAsync(filter, 1, query.MaxRows + 1, ct);
        return ExportLimit.Within(rows.Items, query.MaxRows);
    }
}
