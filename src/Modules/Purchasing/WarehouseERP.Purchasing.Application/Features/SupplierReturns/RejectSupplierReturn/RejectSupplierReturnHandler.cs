using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.RejectSupplierReturn;

// An approver sends a submitted return back to draft with a reason. Returns the new row version.
public sealed partial class RejectSupplierReturnHandler(
    ISupplierReturnRepository returns, ICurrentUser currentUser, TimeProvider clock, ILogger<RejectSupplierReturnHandler> logger)
    : ICommandHandler<RejectSupplierReturnCommand, string>
{
    public async Task<Result<string>> HandleAsync(RejectSupplierReturnCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await ReturnAccess.AuthorizeAsync(returns, currentUser, command.SupplierReturnId, ct);
        if (access.IsFailure)
            return access.Error;
        var supplierReturn = await returns.GetForUpdateAsync(command.SupplierReturnId, expected, ct);
        if (supplierReturn is null)
            return SupplierReturnErrors.NotFound;

        var rejected = supplierReturn.Reject(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (rejected.IsFailure)
            return rejected.Error;
        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ReturnRejected(logger, supplierReturn.Id);
        return Convert.ToBase64String(supplierReturn.RowVersion);
    }

    [LoggerMessage(EventId = 6044, Level = LogLevel.Information, Message = "Supplier return {ReturnId} rejected")]
    private static partial void ReturnRejected(ILogger logger, Guid returnId);
}
