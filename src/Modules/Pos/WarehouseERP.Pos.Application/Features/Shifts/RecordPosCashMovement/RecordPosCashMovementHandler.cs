using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Shifts.RecordPosCashMovement;

// pos.cash.manage (checked by the endpoint), the shift's warehouse in scope, and either the shift's own cashier or a shift
// manager (pos.shifts.manage). The shift is locked at the version the user saw, so a retried click can't move cash twice.
public sealed partial class RecordPosCashMovementHandler(
    ITransactionRunner transactions, IPosShiftRepository shifts, PosReferences references, ICurrentUser currentUser,
    TimeProvider clock, ILogger<RecordPosCashMovementHandler> logger)
    : ICommandHandler<RecordPosCashMovementCommand, string>
{
    public async Task<Result<string>> HandleAsync(RecordPosCashMovementCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = PosAccess.Check(currentUser, await shifts.FindWarehouseAsync(command.ShiftId, ct), ShiftErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        var currency = await references.CurrencyAsync(ct);
        if (currency.IsFailure)
            return currency.Error;

        return await transactions.RunAsync<string>(async token =>
        {
            var shift = await shifts.GetForUpdateAsync(command.ShiftId, expected, token);
            if (shift is null)
                return ShiftErrors.NotFound;
            if (shift.CashierUserId != currentUser.UserId && !currentUser.HasPermission(PosPermissions.ShiftsManage))
                return AccessErrors.NotShiftCashier;
            var moved = shift.MoveCash(command.MovementType, command.Amount, command.Reason, currency.Value.Decimals, currentUser.UserId,
                clock.GetUtcNow().UtcDateTime);
            if (moved.IsFailure)
                return moved.Error;
            var saved = await shifts.SaveChangesAsync(token);
            if (saved.IsFailure)
                return saved.Error;
            CashMoved(logger, shift.Id, command.MovementType, command.Amount);
            return Convert.ToBase64String(shift.RowVersion);
        }, ct);
    }

    [LoggerMessage(EventId = 8021, Level = LogLevel.Information, Message = "POS shift {ShiftId}: {MovementType} {Amount}")]
    private static partial void CashMoved(ILogger logger, Guid shiftId, PosCashMovementType movementType, decimal amount);
}
