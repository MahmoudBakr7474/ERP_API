using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportUnits;

public sealed class ExportUnitsHandler(ICatalogReportReadStore reports) : IQueryHandler<ExportUnitsQuery, IReadOnlyList<UnitUsageResponse>>
{
    public async Task<Result<IReadOnlyList<UnitUsageResponse>>> HandleAsync(ExportUnitsQuery query, CancellationToken ct)
    {
        var rows = await reports.ListUnitUsageAsync(query.IsActive, ct);
        return ExportLimit.Within(rows, query.MaxRows);
    }
}
