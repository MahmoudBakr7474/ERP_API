using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.CancelSupplierReturn;

// A posted return is immutable; only a draft or submitted one is abandoned. Returns the new row version.
public sealed partial class CancelSupplierReturnHandler(
    ISupplierReturnRepository returns, ICurrentUser currentUser, TimeProvider clock, ILogger<CancelSupplierReturnHandler> logger)
    : ICommandHandler<CancelSupplierReturnCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelSupplierReturnCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await ReturnAccess.AuthorizeAsync(returns, currentUser, command.SupplierReturnId, ct);
        if (access.IsFailure)
            return access.Error;
        var supplierReturn = await returns.GetForUpdateAsync(command.SupplierReturnId, expected, ct);
        if (supplierReturn is null)
            return SupplierReturnErrors.NotFound;

        var cancelled = supplierReturn.Cancel(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (cancelled.IsFailure)
            return cancelled.Error;
        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ReturnCancelled(logger, supplierReturn.Id);
        return Convert.ToBase64String(supplierReturn.RowVersion);
    }

    [LoggerMessage(EventId = 6045, Level = LogLevel.Information, Message = "Supplier return {ReturnId} cancelled")]
    private static partial void ReturnCancelled(ILogger logger, Guid returnId);
}
