using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Terminals;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Shared;

// A terminal the user may work at: its warehouse is in the user's scope (scope before existence) and, to sell, it is active.
public sealed class TillAccess(IPosTerminalRepository terminals, ICurrentUser currentUser)
{
    /// <summary>The terminal, untracked, when its warehouse is in the user's scope.</summary>
    public async Task<Result<PosTerminal>> AccessibleAsync(Guid terminalId, CancellationToken ct)
    {
        var terminal = await terminals.FindAsync(terminalId, ct);
        var access = PosAccess.Check(currentUser, terminal?.WarehouseId, TerminalErrors.NotFound);
        return access.IsFailure ? access.Error : terminal!;
    }

    /// <summary>The terminal when it is in the user's scope and active.</summary>
    public async Task<Result<PosTerminal>> UsableAsync(Guid terminalId, CancellationToken ct)
    {
        var terminal = await AccessibleAsync(terminalId, ct);
        if (terminal.IsFailure)
            return terminal.Error;
        return terminal.Value.IsActive ? terminal.Value : TerminalErrors.NotActive;
    }

    /// <summary>The signed-in user; every POS endpoint requires one.</summary>
    public Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("POS actions need a signed-in user.");
}
