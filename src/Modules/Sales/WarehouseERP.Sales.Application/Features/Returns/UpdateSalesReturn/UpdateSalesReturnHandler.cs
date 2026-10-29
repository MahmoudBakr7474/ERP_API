using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Returns.UpdateSalesReturn;

// Only a draft, only from the version the editor read; the lines are checked again against what may still come back.
// Returns the new row version.
public sealed partial class UpdateSalesReturnHandler(
    ISalesReturnRepository returns, ISalesDeliveryRepository deliveries, SalesReferences references, ICurrentUser currentUser,
    ILogger<UpdateSalesReturnHandler> logger)
    : ICommandHandler<UpdateSalesReturnCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateSalesReturnCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await ReturnAccess.AuthorizeAsync(returns, currentUser, command.ReturnId, ct);
        if (access.IsFailure)
            return access.Error;
        var salesReturn = await returns.GetForUpdateAsync(command.ReturnId, expected, ct);
        if (salesReturn is null)
            return ReturnErrors.NotFound;
        var delivery = await deliveries.GetForUpdateAsync(salesReturn.DeliveryId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Return {salesReturn.Id} points at delivery {salesReturn.DeliveryId}, which does not exist.");
        var lines = await ReturnLines.ResolveAsync(references, delivery, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;
        var replaced = salesReturn.ReplaceDraft(delivery, command.Reason, command.Notes, lines.Value);
        if (replaced.IsFailure)
            return replaced.Error;
        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ReturnUpdated(logger, salesReturn.Id);
        return Convert.ToBase64String(salesReturn.RowVersion);
    }

    [LoggerMessage(EventId = 7061, Level = LogLevel.Information, Message = "Customer return {ReturnId} updated")]
    private static partial void ReturnUpdated(ILogger logger, Guid returnId);
}
