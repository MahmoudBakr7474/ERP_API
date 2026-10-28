using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Contracts.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers.GetStockTransfer;

public sealed class GetStockTransferHandler(
    IStockTransferRepository transfers, IStockTransferReadStore readStore, ICurrentUser currentUser)
    : IQueryHandler<GetStockTransferQuery, StockTransferDetailsResponse>
{
    public async Task<Result<StockTransferDetailsResponse>> HandleAsync(GetStockTransferQuery query, CancellationToken ct)
    {
        var access = await TransferAccess.AuthorizeAsync(transfers, currentUser, query.TransferId, TransferAccess.Side.Either, ct);
        if (access.IsFailure)
            return access.Error;
        var details = await readStore.GetAsync(query.TransferId, ct);
        if (details is null)
            return StockTransferErrors.NotFound;
        return details;
    }
}
