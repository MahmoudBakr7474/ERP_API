using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Returns.SubmitSalesReturn;

// A return always goes to an approver; its creator may approve it. Returns the new row version.
public sealed partial class SubmitSalesReturnHandler(
    ISalesReturnRepository returns, ICurrentUser currentUser, TimeProvider clock, ILogger<SubmitSalesReturnHandler> logger)
    : ICommandHandler<SubmitSalesReturnCommand, string>
{
    public async Task<Result<string>> HandleAsync(SubmitSalesReturnCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await ReturnAccess.AuthorizeAsync(returns, currentUser, command.ReturnId, ct);
        if (access.IsFailure)
            return access.Error;
        var salesReturn = await returns.GetForUpdateAsync(command.ReturnId, expected, ct);
        if (salesReturn is null)
            return ReturnErrors.NotFound;
        var submitted = salesReturn.Submit(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (submitted.IsFailure)
            return submitted.Error;
        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ReturnSubmitted(logger, salesReturn.Id);
        return Convert.ToBase64String(salesReturn.RowVersion);
    }

    [LoggerMessage(EventId = 7062, Level = LogLevel.Information, Message = "Customer return {ReturnId} submitted")]
    private static partial void ReturnSubmitted(ILogger logger, Guid returnId);
}
