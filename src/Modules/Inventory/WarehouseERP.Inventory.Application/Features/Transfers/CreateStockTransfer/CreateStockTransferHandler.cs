using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers.CreateStockTransfer;

// A draft transfer out of a physical warehouse in the user's scope to another physical warehouse, through a transit
// warehouse. It copies the dispatch approval policy now. One save point.
public sealed partial class CreateStockTransferHandler(
    IStockTransferRepository transfers, IDocumentPolicyRepository policies, StockReferences references,
    ICurrentUser currentUser, TimeProvider clock, ILogger<CreateStockTransferHandler> logger)
    : ICommandHandler<CreateStockTransferCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateStockTransferCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.SourceWarehouseId))
            return StockErrors.WarehouseAccessDenied;
        var source = await references.UsableWarehouseAsync(command.SourceWarehouseId, ct);
        if (source.IsFailure)
            return StockTransferErrors.SourceNotUsable;
        var destination = await references.UsableWarehouseAsync(command.DestinationWarehouseId, ct);
        if (destination.IsFailure)
            return StockTransferErrors.DestinationNotUsable;
        var transit = await references.TransitWarehouseAsync(command.TransitWarehouseId, source.Value.BranchId, ct);
        if (transit.IsFailure)
            return transit.Error;
        var lines = await references.ResolveTransferLinesAsync(command.SourceWarehouseId, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;

        var requiresApproval = await policies.RequiresApprovalAsync(StockDocumentType.TransferDispatch, ct);
        var created = StockTransfer.CreateDraft(
            source.Value.Id, source.Value.BranchId, destination.Value.Id, destination.Value.BranchId, transit.Value.Id,
            requiresApproval, command.ExternalReference, command.Notes, lines.Value, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;

        transfers.Add(created.Value);
        var saved = await transfers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        TransferCreated(logger, created.Value.Id);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 5021, Level = LogLevel.Information, Message = "Transfer {TransferId} drafted")]
    private static partial void TransferCreated(ILogger logger, Guid transferId);
}
