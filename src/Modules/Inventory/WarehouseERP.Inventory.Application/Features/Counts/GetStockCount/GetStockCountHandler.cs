using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Contracts.Counts;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Counts.GetStockCount;

public sealed class GetStockCountHandler(IStockCountRepository counts, IStockCountReadStore readStore, ICurrentUser currentUser)
    : IQueryHandler<GetStockCountQuery, StockCountDetailsResponse>
{
    public async Task<Result<StockCountDetailsResponse>> HandleAsync(GetStockCountQuery query, CancellationToken ct)
    {
        var access = await CountAccess.AuthorizeAsync(counts, currentUser, query.CountId, ct);
        if (access.IsFailure)
            return access.Error;
        var details = await readStore.GetAsync(query.CountId, ct);
        if (details is null)
            return StockCountErrors.NotFound;
        return details;
    }
}
