using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers.CancelStockTransfer;

// A draft or submitted transfer is abandoned; a dispatched one is received or closed. Returns the new row version.
public sealed partial class CancelStockTransferHandler(
    IStockTransferRepository transfers, ICurrentUser currentUser, TimeProvider clock, ILogger<CancelStockTransferHandler> logger)
    : ICommandHandler<CancelStockTransferCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelStockTransferCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await TransferAccess.AuthorizeAsync(transfers, currentUser, command.TransferId, TransferAccess.Side.Source, ct);
        if (access.IsFailure)
            return access.Error;
        var transfer = await transfers.GetForUpdateAsync(command.TransferId, expected, ct);
        if (transfer is null)
            return StockTransferErrors.NotFound;

        var cancelled = transfer.Cancel(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (cancelled.IsFailure)
            return cancelled.Error;
        var saved = await transfers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        TransferCancelled(logger, transfer.Id);
        return Convert.ToBase64String(transfer.RowVersion);
    }

    [LoggerMessage(EventId = 5025, Level = LogLevel.Information, Message = "Transfer {TransferId} cancelled")]
    private static partial void TransferCancelled(ILogger logger, Guid transferId);
}
