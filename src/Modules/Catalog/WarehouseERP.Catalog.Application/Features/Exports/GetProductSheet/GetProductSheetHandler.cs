using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Exports.GetProductSheet;

public sealed class GetProductSheetHandler(ICatalogReportReadStore reports) : IQueryHandler<GetProductSheetQuery, ProductSheetResponse>
{
    public async Task<Result<ProductSheetResponse>> HandleAsync(GetProductSheetQuery query, CancellationToken ct) =>
        await reports.GetProductSheetAsync(query.ProductId, ct) is { } sheet ? sheet : ProductErrors.NotFound;
}
