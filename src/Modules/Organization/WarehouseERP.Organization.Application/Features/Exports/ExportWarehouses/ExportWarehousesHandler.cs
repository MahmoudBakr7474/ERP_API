using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportWarehouses;

// The same read as the warehouse list (scope, filters, order, counts), over the whole result.
public sealed class ExportWarehousesHandler(IWarehouseReadStore warehouses, ICurrentUser user)
    : IQueryHandler<ExportWarehousesQuery, IReadOnlyList<WarehouseSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<WarehouseSummaryResponse>>> HandleAsync(ExportWarehousesQuery query, CancellationToken ct)
    {
        var filter = new WarehouseListFilter(
            query.BranchId, WarehouseScope.Of(user), Text.Optional(query.Search), query.IsActive, query.Type, query.AllowsSelling,
            query.Sort ?? OrganizationSort.Code, query.Descending);
        var rows = await warehouses.ListAsync(filter, 1, query.MaxRows + 1, ct);
        return ExportLimit.Within(rows.Items, query.MaxRows);
    }
}
