using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Sales.Application.Contracts.Returns;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Returns.GetSalesReturn;

public sealed class GetSalesReturnHandler(ISalesReturnRepository returns, ISalesReturnReadStore readStore, ICurrentUser currentUser)
    : IQueryHandler<GetSalesReturnQuery, SalesReturnDetailsResponse>
{
    public async Task<Result<SalesReturnDetailsResponse>> HandleAsync(GetSalesReturnQuery query, CancellationToken ct)
    {
        var access = await ReturnAccess.AuthorizeAsync(returns, currentUser, query.ReturnId, ct);
        if (access.IsFailure)
            return access.Error;
        return await readStore.GetAsync(query.ReturnId, ct) is { } salesReturn ? salesReturn : ReturnErrors.NotFound;
    }
}
