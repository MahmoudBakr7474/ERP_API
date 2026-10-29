using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;
using WarehouseERP.Pos.Application.Contracts.Shifts;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Shifts.ClosePosShift;

// The shift's cashier closes it with pos.sell; anyone else needs pos.shifts.manage and closes it as forced, with a note
// (an abandoned shift). The shift is locked at the version the user saw, so no sale or cash movement slips in between the
// count and the close. Returns the closed shift's report (Z report).
public sealed partial class ClosePosShiftHandler(
    ITransactionRunner transactions, IPosShiftRepository shifts, IPosShiftReadStore reads, PosReferences references,
    ICurrentUser currentUser, TimeProvider clock, ILogger<ClosePosShiftHandler> logger)
    : ICommandHandler<ClosePosShiftCommand, PosShiftResponse>
{
    public async Task<Result<PosShiftResponse>> HandleAsync(ClosePosShiftCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = PosAccess.Check(currentUser, await shifts.FindWarehouseAsync(command.ShiftId, ct), ShiftErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        var currency = await references.CurrencyAsync(ct);
        if (currency.IsFailure)
            return currency.Error;

        return await transactions.RunAsync<PosShiftResponse>(async token =>
        {
            var shift = await shifts.GetForUpdateAsync(command.ShiftId, expected, token);
            if (shift is null)
                return ShiftErrors.NotFound;
            var forced = shift.CashierUserId != currentUser.UserId;
            var needed = forced ? PosPermissions.ShiftsManage : PosPermissions.Sell;
            if (!currentUser.HasPermission(needed))
                return forced ? AccessErrors.NotShiftCashier : AccessErrors.PermissionDenied(needed);
            var closed = shift.Close(command.CountedCash, command.Note, forced, currency.Value.Decimals, currentUser.UserId,
                clock.GetUtcNow().UtcDateTime);
            if (closed.IsFailure)
                return closed.Error;
            var saved = await shifts.SaveChangesAsync(token);
            if (saved.IsFailure)
                return saved.Error;
            ShiftClosed(logger, shift.Id, shift.Number, forced, shift.CashDifference ?? 0m);
            return await reads.GetAsync(shift.Id, token)
                ?? throw new InvalidOperationException($"Shift {shift.Id} was closed but can't be read back.");
        }, ct);
    }

    [LoggerMessage(EventId = 8022, Level = LogLevel.Information,
        Message = "POS shift {ShiftId} ({Number}) closed (forced: {Forced}), cash difference {CashDifference}")]
    private static partial void ShiftClosed(ILogger logger, Guid shiftId, string number, bool forced, decimal cashDifference);
}
