using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Terminals;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Terminals.SetPosTerminalActive;

// A terminal with an open shift can't be retired (its cashier must count and close first); one comes back only while its
// warehouse still allows selling.
public sealed partial class SetPosTerminalActiveHandler(
    IPosTerminalRepository terminals, IPosShiftRepository shifts, PosReferences references, ICurrentUser currentUser,
    ILogger<SetPosTerminalActiveHandler> logger)
    : ICommandHandler<SetPosTerminalActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetPosTerminalActiveCommand command, CancellationToken ct)
    {
        var terminal = await terminals.GetForUpdateAsync(command.TerminalId, expectedRowVersion: null, ct);
        var access = PosAccess.Check(currentUser, terminal?.WarehouseId, TerminalErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        if (command.IsActive)
        {
            var warehouse = await references.SellingWarehouseAsync(terminal!.WarehouseId, ct);
            if (warehouse.IsFailure)
                return warehouse.Error;
            terminal.Activate();
        }
        else
        {
            if (await shifts.HasOpenAsync(terminal!.Id, ct))
                return TerminalErrors.HasOpenShift;
            terminal.Deactivate();
        }
        var saved = await terminals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        TerminalActiveSet(logger, terminal.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 8012, Level = LogLevel.Information, Message = "POS terminal {TerminalId} active: {IsActive}")]
    private static partial void TerminalActiveSet(ILogger logger, Guid terminalId, bool isActive);
}
