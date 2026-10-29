using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Terminals;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Terminals.UpdatePosTerminal;

// From the version the editor read. A new selling location applies to the next sale; sales already made keep theirs.
public sealed partial class UpdatePosTerminalHandler(
    IPosTerminalRepository terminals, PosReferences references, ICurrentUser currentUser, ILogger<UpdatePosTerminalHandler> logger)
    : ICommandHandler<UpdatePosTerminalCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdatePosTerminalCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var terminal = await terminals.GetForUpdateAsync(command.TerminalId, expected, ct);
        var access = PosAccess.Check(currentUser, terminal?.WarehouseId, TerminalErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        var location = await references.SellingLocationAsync(terminal!.WarehouseId, command.SellingLocationId, ct);
        if (location.IsFailure)
            return location.Error;
        var updated = terminal.Update(command.Name, location.Value, command.AcceptsCash, command.AcceptsCard, command.ReceiptHeader,
            command.ReceiptFooter);
        if (updated.IsFailure)
            return updated.Error;
        var saved = await terminals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        TerminalUpdated(logger, terminal.Id, terminal.Code);
        return Convert.ToBase64String(terminal.RowVersion);
    }

    [LoggerMessage(EventId = 8011, Level = LogLevel.Information, Message = "POS terminal {TerminalId} ({Code}) updated")]
    private static partial void TerminalUpdated(ILogger logger, Guid terminalId, string code);
}
