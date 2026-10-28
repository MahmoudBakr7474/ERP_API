using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Contracts.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers.ApproveStockTransfer;

// An approver with the source warehouse in scope, who did not create the submitted transfer, approves it: it dispatches.
public sealed class ApproveStockTransferHandler(
    IStockTransferRepository transfers, TransferDispatching dispatching, ICurrentUser currentUser)
    : ICommandHandler<ApproveStockTransferCommand, StockTransferPostedResponse>
{
    public async Task<Result<StockTransferPostedResponse>> HandleAsync(ApproveStockTransferCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await TransferAccess.AuthorizeAsync(transfers, currentUser, command.TransferId, TransferAccess.Side.Source, ct);
        if (access.IsFailure)
            return access.Error;
        return await dispatching.DispatchAsync(command.TransferId, command.IdempotencyKey, expected, byApproval: true, ct);
    }
}
