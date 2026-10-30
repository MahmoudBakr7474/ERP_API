using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportProductUnits;

public sealed class ExportProductUnitsHandler(ICatalogReportReadStore reports) : IQueryHandler<ExportProductUnitsQuery, IReadOnlyList<UnitConversionResponse>>
{
    public async Task<Result<IReadOnlyList<UnitConversionResponse>>> HandleAsync(ExportProductUnitsQuery query, CancellationToken ct)
    {
        var filter = new UnitConversionFilter(Text.Optional(query.Search), query.CategoryId, query.Type, query.ProductIsActive, query.UnitId);
        var rows = await reports.ListUnitConversionsAsync(filter, 1, query.MaxRows + 1, ct);
        return ExportLimit.Within(rows.Items, query.MaxRows);
    }
}
