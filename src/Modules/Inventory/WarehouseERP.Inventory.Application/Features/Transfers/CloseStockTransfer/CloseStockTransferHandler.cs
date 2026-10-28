using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers.CloseStockTransfer;

// What never arrived is not cleared from a number: a loss adjustment out of the transfer's transit location is submitted
// for approval and receiving stops. Approving that adjustment (any approver, its creator included) closes the transfer; cancelling it lets
// receiving go on. Returns the adjustment's id.
public sealed partial class CloseStockTransferHandler(
    IStockTransferRepository transfers, IStockDocumentRepository documents, StockReferences references,
    IWarehouseDirectory warehouses, ICurrentUser currentUser, TimeProvider clock, ILogger<CloseStockTransferHandler> logger)
    : ICommandHandler<CloseStockTransferCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CloseStockTransferCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await TransferAccess.AuthorizeAsync(transfers, currentUser, command.TransferId, TransferAccess.Side.Either, ct);
        if (access.IsFailure)
            return access.Error;
        var transfer = await transfers.GetForUpdateAsync(command.TransferId, expected, ct);
        if (transfer is null)
            return StockTransferErrors.NotFound;
        if (transfer.Status is not (StockTransferStatus.InTransit or StockTransferStatus.PartiallyReceived))
            return StockTransferDomainErrors.NotInTransit;
        if (transfer.OutstandingQuantity == 0)
            return StockTransferDomainErrors.NothingOutstanding;

        var transit = await warehouses.FindAsync(transfer.TransitWarehouseId, ct)
            ?? throw new InvalidOperationException($"Transfer {transfer.Id} goes through a warehouse that does not exist.");
        var outstanding = transfer.Lines.Where(l => l.OutstandingQuantity > 0).OrderBy(l => l.LineNumber).ToList();
        var baseUnits = await references.BaseUnitsAsync(outstanding.Select(l => l.ProductId).Distinct().ToList(), ct);
        var place = new StockPlace(transfer.TransitWarehouseId, transfer.TransitLocationId!.Value);
        var lines = outstanding
            .Select(l => new GeneratedLineInput(
                l.ProductId, baseUnits[l.ProductId].Id, baseUnits[l.ProductId].UnitId, l.OutstandingQuantity, StockDirection.Out,
                UnitCost: null, place))
            .ToList();

        var now = clock.GetUtcNow().UtcDateTime;
        var loss = StockDocument.CreateTransferLoss(transfer, transit.BranchId, command.Reason, command.Notes, lines, currentUser.UserId, now);
        var closing = transfer.BeginClosing(command.Notes, currentUser.UserId, now);
        if (closing.IsFailure)
            return closing.Error;
        documents.Add(loss);
        var saved = await transfers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ClosingRequested(logger, transfer.Id, loss.Id);
        return loss.Id;
    }

    [LoggerMessage(EventId = 5027, Level = LogLevel.Information, Message = "Transfer {TransferId} closing with loss adjustment {DocumentId}")]
    private static partial void ClosingRequested(ILogger logger, Guid transferId, Guid documentId);
}
