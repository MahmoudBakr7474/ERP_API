using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers.RejectStockTransfer;

// An approver with the source warehouse in scope sends a submitted transfer back to draft with a reason.
public sealed partial class RejectStockTransferHandler(
    IStockTransferRepository transfers, ICurrentUser currentUser, TimeProvider clock, ILogger<RejectStockTransferHandler> logger)
    : ICommandHandler<RejectStockTransferCommand, string>
{
    public async Task<Result<string>> HandleAsync(RejectStockTransferCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await TransferAccess.AuthorizeAsync(transfers, currentUser, command.TransferId, TransferAccess.Side.Source, ct);
        if (access.IsFailure)
            return access.Error;
        var transfer = await transfers.GetForUpdateAsync(command.TransferId, expected, ct);
        if (transfer is null)
            return StockTransferErrors.NotFound;

        var rejected = transfer.Reject(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (rejected.IsFailure)
            return rejected.Error;
        var saved = await transfers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        TransferRejected(logger, transfer.Id);
        return Convert.ToBase64String(transfer.RowVersion);
    }

    [LoggerMessage(EventId = 5024, Level = LogLevel.Information, Message = "Transfer {TransferId} rejected")]
    private static partial void TransferRejected(ILogger logger, Guid transferId);
}
