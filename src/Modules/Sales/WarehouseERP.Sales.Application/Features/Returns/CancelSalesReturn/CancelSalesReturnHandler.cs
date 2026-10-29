using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Returns.CancelSalesReturn;

// A draft or submitted return is abandoned; nothing came back into stock. Returns the new row version.
public sealed partial class CancelSalesReturnHandler(
    ISalesReturnRepository returns, ICurrentUser currentUser, TimeProvider clock, ILogger<CancelSalesReturnHandler> logger)
    : ICommandHandler<CancelSalesReturnCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelSalesReturnCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await ReturnAccess.AuthorizeAsync(returns, currentUser, command.ReturnId, ct);
        if (access.IsFailure)
            return access.Error;
        var salesReturn = await returns.GetForUpdateAsync(command.ReturnId, expected, ct);
        if (salesReturn is null)
            return ReturnErrors.NotFound;
        var cancelled = salesReturn.Cancel(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (cancelled.IsFailure)
            return cancelled.Error;
        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ReturnCancelled(logger, salesReturn.Id);
        return Convert.ToBase64String(salesReturn.RowVersion);
    }

    [LoggerMessage(EventId = 7065, Level = LogLevel.Information, Message = "Customer return {ReturnId} cancelled")]
    private static partial void ReturnCancelled(ILogger logger, Guid returnId);
}
