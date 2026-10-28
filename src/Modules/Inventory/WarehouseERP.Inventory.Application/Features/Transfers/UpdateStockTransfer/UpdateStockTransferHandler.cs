using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers.UpdateStockTransfer;

// Only a draft changes, and only from the version its editor read (409 otherwise). Returns the new row version.
public sealed partial class UpdateStockTransferHandler(
    IStockTransferRepository transfers, StockReferences references, ICurrentUser currentUser,
    ILogger<UpdateStockTransferHandler> logger)
    : ICommandHandler<UpdateStockTransferCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateStockTransferCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await TransferAccess.AuthorizeAsync(transfers, currentUser, command.TransferId, TransferAccess.Side.Source, ct);
        if (access.IsFailure)
            return access.Error;
        var transfer = await transfers.GetForUpdateAsync(command.TransferId, expected, ct);
        if (transfer is null)
            return StockTransferErrors.NotFound;

        var transit = await references.TransitWarehouseAsync(command.TransitWarehouseId ?? transfer.TransitWarehouseId, transfer.SourceBranchId, ct);
        if (transit.IsFailure)
            return transit.Error;
        var lines = await references.ResolveTransferLinesAsync(transfer.SourceWarehouseId, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;
        var replaced = transfer.ReplaceDraft(transit.Value.Id, command.ExternalReference, command.Notes, lines.Value);
        if (replaced.IsFailure)
            return replaced.Error;
        var saved = await transfers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        TransferUpdated(logger, transfer.Id);
        return Convert.ToBase64String(transfer.RowVersion);
    }

    [LoggerMessage(EventId = 5022, Level = LogLevel.Information, Message = "Transfer {TransferId} draft updated")]
    private static partial void TransferUpdated(ILogger logger, Guid transferId);
}
