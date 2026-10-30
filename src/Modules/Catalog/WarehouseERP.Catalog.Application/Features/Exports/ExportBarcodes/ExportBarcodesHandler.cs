using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportBarcodes;

public sealed class ExportBarcodesHandler(ICatalogReportReadStore reports) : IQueryHandler<ExportBarcodesQuery, IReadOnlyList<BarcodeReportResponse>>
{
    public async Task<Result<IReadOnlyList<BarcodeReportResponse>>> HandleAsync(ExportBarcodesQuery query, CancellationToken ct)
    {
        var filter = new BarcodeReportFilter(
            Text.Optional(query.Search), query.CategoryId, query.Type, query.ProductIsActive, query.BarcodeType, query.IsActive);
        var rows = await reports.ListBarcodesAsync(filter, 1, query.MaxRows + 1, ct);
        return ExportLimit.Within(rows.Items, query.MaxRows);
    }
}
