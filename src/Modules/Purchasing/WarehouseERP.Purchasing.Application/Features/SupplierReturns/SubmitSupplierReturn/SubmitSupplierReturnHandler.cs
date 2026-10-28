using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.SubmitSupplierReturn;

// Every return goes to the approvers; approving it is what posts it. Returns the new row version.
public sealed partial class SubmitSupplierReturnHandler(
    ISupplierReturnRepository returns, ICurrentUser currentUser, TimeProvider clock, ILogger<SubmitSupplierReturnHandler> logger)
    : ICommandHandler<SubmitSupplierReturnCommand, string>
{
    public async Task<Result<string>> HandleAsync(SubmitSupplierReturnCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await ReturnAccess.AuthorizeAsync(returns, currentUser, command.SupplierReturnId, ct);
        if (access.IsFailure)
            return access.Error;
        var supplierReturn = await returns.GetForUpdateAsync(command.SupplierReturnId, expected, ct);
        if (supplierReturn is null)
            return SupplierReturnErrors.NotFound;

        var submitted = supplierReturn.Submit(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (submitted.IsFailure)
            return submitted.Error;
        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ReturnSubmitted(logger, supplierReturn.Id);
        return Convert.ToBase64String(supplierReturn.RowVersion);
    }

    [LoggerMessage(EventId = 6042, Level = LogLevel.Information, Message = "Supplier return {ReturnId} submitted for approval")]
    private static partial void ReturnSubmitted(ILogger logger, Guid returnId);
}
