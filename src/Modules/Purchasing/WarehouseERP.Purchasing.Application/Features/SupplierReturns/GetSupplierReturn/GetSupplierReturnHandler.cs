using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;
using WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.GetSupplierReturn;

public sealed class GetSupplierReturnHandler(ISupplierReturnRepository returns, ISupplierReturnReadStore readStore, ICurrentUser currentUser)
    : IQueryHandler<GetSupplierReturnQuery, SupplierReturnDetailsResponse>
{
    public async Task<Result<SupplierReturnDetailsResponse>> HandleAsync(GetSupplierReturnQuery query, CancellationToken ct)
    {
        var access = await ReturnAccess.AuthorizeAsync(returns, currentUser, query.SupplierReturnId, ct);
        if (access.IsFailure)
            return access.Error;
        return await readStore.GetAsync(query.SupplierReturnId, ct) is { } supplierReturn ? supplierReturn : SupplierReturnErrors.NotFound;
    }
}
