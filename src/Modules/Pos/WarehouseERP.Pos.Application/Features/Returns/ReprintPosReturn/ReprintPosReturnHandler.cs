using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Pos.Application.Contracts.Returns;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Returns.ReprintPosReturn;

// A copy of a return receipt (pos.receipts.reprint, checked by the endpoint), counted and marked as a copy.
public sealed partial class ReprintPosReturnHandler(
    IPosReturnRepository returns, IPosReturnReadStore reads, ICurrentUser currentUser, TimeProvider clock,
    ILogger<ReprintPosReturnHandler> logger)
    : ICommandHandler<ReprintPosReturnCommand, PosReturnResponse>
{
    public async Task<Result<PosReturnResponse>> HandleAsync(ReprintPosReturnCommand command, CancellationToken ct)
    {
        var access = PosAccess.Check(currentUser, await returns.FindWarehouseAsync(command.ReturnId, ct), ReturnErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        var posReturn = await returns.GetForUpdateAsync(command.ReturnId, ct);
        if (posReturn is null)
            return ReturnErrors.NotFound;
        var printed = posReturn.RecordReprint(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ReceiptReprinted(logger, posReturn.Id, posReturn.Number, printed - 1);
        var receipt = await reads.GetAsync(posReturn.Id, ct) ?? throw new InvalidOperationException($"Return {posReturn.Id} can't be read back.");
        return receipt with { IsCopy = true, CopyNumber = printed - 1 };
    }

    [LoggerMessage(EventId = 8041, Level = LogLevel.Information, Message = "POS return {ReturnId} ({Number}): copy {CopyNumber} printed")]
    private static partial void ReceiptReprinted(ILogger logger, Guid returnId, string number, int copyNumber);
}
