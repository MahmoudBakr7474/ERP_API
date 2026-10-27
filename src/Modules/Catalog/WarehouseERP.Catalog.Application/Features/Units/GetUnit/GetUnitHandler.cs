using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Units;
using WarehouseERP.Catalog.Application.Contracts.Units;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Units.GetUnit;

public sealed class GetUnitHandler(IUnitReadStore units) : IQueryHandler<GetUnitQuery, UnitDetailsResponse>
{
    public async Task<Result<UnitDetailsResponse>> HandleAsync(GetUnitQuery query, CancellationToken ct) =>
        await units.GetAsync(query.UnitId, ct) is { } unit ? unit : UnitErrors.NotFound;
}
