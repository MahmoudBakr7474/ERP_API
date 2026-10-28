using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers.SubmitStockTransfer;

// A draft whose dispatch needs approval waits for an approver. Returns the new row version.
public sealed partial class SubmitStockTransferHandler(
    IStockTransferRepository transfers, ICurrentUser currentUser, TimeProvider clock, ILogger<SubmitStockTransferHandler> logger)
    : ICommandHandler<SubmitStockTransferCommand, string>
{
    public async Task<Result<string>> HandleAsync(SubmitStockTransferCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await TransferAccess.AuthorizeAsync(transfers, currentUser, command.TransferId, TransferAccess.Side.Source, ct);
        if (access.IsFailure)
            return access.Error;
        var transfer = await transfers.GetForUpdateAsync(command.TransferId, expected, ct);
        if (transfer is null)
            return StockTransferErrors.NotFound;

        var submitted = transfer.Submit(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (submitted.IsFailure)
            return submitted.Error;
        var saved = await transfers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        TransferSubmitted(logger, transfer.Id);
        return Convert.ToBase64String(transfer.RowVersion);
    }

    [LoggerMessage(EventId = 5023, Level = LogLevel.Information, Message = "Transfer {TransferId} submitted for approval")]
    private static partial void TransferSubmitted(ILogger logger, Guid transferId);
}
