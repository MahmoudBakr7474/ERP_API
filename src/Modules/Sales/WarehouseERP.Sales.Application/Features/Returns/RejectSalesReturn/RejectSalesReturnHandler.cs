using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Returns.RejectSalesReturn;

// An approver sends a submitted return back to draft with a reason. Returns the new row version.
public sealed partial class RejectSalesReturnHandler(
    ISalesReturnRepository returns, ICurrentUser currentUser, TimeProvider clock, ILogger<RejectSalesReturnHandler> logger)
    : ICommandHandler<RejectSalesReturnCommand, string>
{
    public async Task<Result<string>> HandleAsync(RejectSalesReturnCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await ReturnAccess.AuthorizeAsync(returns, currentUser, command.ReturnId, ct);
        if (access.IsFailure)
            return access.Error;
        var salesReturn = await returns.GetForUpdateAsync(command.ReturnId, expected, ct);
        if (salesReturn is null)
            return ReturnErrors.NotFound;
        var rejected = salesReturn.Reject(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (rejected.IsFailure)
            return rejected.Error;
        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ReturnRejected(logger, salesReturn.Id);
        return Convert.ToBase64String(salesReturn.RowVersion);
    }

    [LoggerMessage(EventId = 7064, Level = LogLevel.Information, Message = "Customer return {ReturnId} rejected")]
    private static partial void ReturnRejected(ILogger logger, Guid returnId);
}
