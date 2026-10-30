using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportCategories;

public sealed class ExportCategoriesHandler(ICatalogReportReadStore reports) : IQueryHandler<ExportCategoriesQuery, IReadOnlyList<CategoryProductCountResponse>>
{
    public async Task<Result<IReadOnlyList<CategoryProductCountResponse>>> HandleAsync(ExportCategoriesQuery query, CancellationToken ct)
    {
        var rows = await reports.ListCategoryProductCountsAsync(query.IsActive, ct);
        return ExportLimit.Within(rows, query.MaxRows);
    }
}
