using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Shifts.OpenPosShift;

// The caller becomes the shift's cashier (pos.sell, checked by the endpoint) at an active terminal of their scope whose
// warehouse still allows selling. One open shift per terminal and per cashier: checked first for a clear answer, and backed
// by unique indexes, so two cashiers racing for one till can't both open. Numbered in the same transaction.
public sealed partial class OpenPosShiftHandler(
    ITransactionRunner transactions, TillAccess tills, IPosShiftRepository shifts, PosReferences references,
    DocumentNumbering numbering, TimeProvider clock, ILogger<OpenPosShiftHandler> logger)
    : ICommandHandler<OpenPosShiftCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(OpenPosShiftCommand command, CancellationToken ct)
    {
        var terminal = await tills.UsableAsync(command.TerminalId, ct);
        if (terminal.IsFailure)
            return terminal.Error;
        var warehouse = await references.SellingWarehouseAsync(terminal.Value.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        var currency = await references.CurrencyAsync(ct);
        if (currency.IsFailure)
            return currency.Error;
        if (await shifts.HasOpenAsync(terminal.Value.Id, ct))
            return ShiftErrors.TerminalBusy;
        var cashierId = tills.UserId;

        return await transactions.RunAsync<Guid>(async token =>
        {
            var number = await numbering.NextAsync(PosDocumentType.Shift, terminal.Value, token);
            if (number.IsFailure)
                return number.Error;
            var opened = PosShift.Open(number.Value, terminal.Value, cashierId, command.OpeningFloat, currency.Value.Decimals,
                clock.GetUtcNow().UtcDateTime);
            if (opened.IsFailure)
                return opened.Error;
            shifts.Add(opened.Value);
            var saved = await shifts.SaveChangesAsync(token);
            if (saved.IsFailure)
                return saved.Error;
            ShiftOpened(logger, opened.Value.Id, opened.Value.Number, terminal.Value.Code, command.OpeningFloat);
            return opened.Value.Id;
        }, ct);
    }

    [LoggerMessage(EventId = 8020, Level = LogLevel.Information,
        Message = "POS shift {ShiftId} ({Number}) opened at terminal {TerminalCode} with float {OpeningFloat}")]
    private static partial void ShiftOpened(ILogger logger, Guid shiftId, string number, string terminalCode, decimal openingFloat);
}
