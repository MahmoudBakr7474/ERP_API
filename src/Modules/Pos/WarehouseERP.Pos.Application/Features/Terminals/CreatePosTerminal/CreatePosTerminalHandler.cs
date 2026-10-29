using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Terminals;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Terminals.CreatePosTerminal;

// A till in a warehouse of the user's scope (pos.terminals.manage, checked by the endpoint): a physical, active warehouse
// that allows selling, and an active location of it to sell from. The code is unique (the database backs the check).
public sealed partial class CreatePosTerminalHandler(
    IPosTerminalRepository terminals, PosReferences references, ICurrentUser currentUser, ILogger<CreatePosTerminalHandler> logger)
    : ICommandHandler<CreatePosTerminalCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreatePosTerminalCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return AccessErrors.WarehouseDenied;
        var warehouse = await references.SellingWarehouseAsync(command.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        var location = await references.SellingLocationAsync(command.WarehouseId, command.SellingLocationId, ct);
        if (location.IsFailure)
            return location.Error;

        var created = PosTerminal.Create(command.Code, command.Name, warehouse.Value.BranchId, command.WarehouseId, location.Value,
            command.AcceptsCash, command.AcceptsCard, command.ReceiptHeader, command.ReceiptFooter);
        if (created.IsFailure)
            return created.Error;
        if (await terminals.CodeExistsAsync(created.Value.Code, ct))
            return TerminalErrors.CodeTaken;
        terminals.Add(created.Value);
        var saved = await terminals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        TerminalCreated(logger, created.Value.Id, created.Value.Code, command.WarehouseId);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 8010, Level = LogLevel.Information, Message = "POS terminal {TerminalId} ({Code}) registered in warehouse {WarehouseId}")]
    private static partial void TerminalCreated(ILogger logger, Guid terminalId, string code, Guid warehouseId);
}
