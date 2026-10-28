using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Contracts.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers.DispatchStockTransfer;

// Direct dispatch of a draft whose dispatch needs no approval, by someone who manages transfers of its source warehouse.
public sealed class DispatchStockTransferHandler(
    IStockTransferRepository transfers, TransferDispatching dispatching, ICurrentUser currentUser)
    : ICommandHandler<DispatchStockTransferCommand, StockTransferPostedResponse>
{
    public async Task<Result<StockTransferPostedResponse>> HandleAsync(DispatchStockTransferCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await TransferAccess.AuthorizeAsync(transfers, currentUser, command.TransferId, TransferAccess.Side.Source, ct);
        if (access.IsFailure)
            return access.Error;
        return await dispatching.DispatchAsync(command.TransferId, command.IdempotencyKey, expected, byApproval: false, ct);
    }
}
