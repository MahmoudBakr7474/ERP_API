using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Sales;
using WarehouseERP.Pos.Application.Contracts.Sales;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Sales.GetPosSale;

// A sale to look at (or to return goods against). It was printed at checkout, so it reads as a copy; printing it again
// goes through the reprint, which counts copies.
public sealed class GetPosSaleHandler(IPosSaleReadStore sales, ICurrentUser currentUser) : IQueryHandler<GetPosSaleQuery, PosSaleResponse>
{
    public async Task<Result<PosSaleResponse>> HandleAsync(GetPosSaleQuery query, CancellationToken ct)
    {
        var sale = await sales.GetAsync(query.SaleId, ct);
        var access = PosAccess.Check(currentUser, sale?.WarehouseId, SaleErrors.NotFound);
        return access.IsFailure ? access.Error : sale!;
    }
}
