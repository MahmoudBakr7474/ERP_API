using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ClosePurchaseRequest;

// Closes an approved request: what is not on order yet will never be ordered from it; orders already drawn keep their
// quantities. Returns the new row version.
public sealed partial class ClosePurchaseRequestHandler(
    IPurchaseRequestRepository requests, ICurrentUser currentUser, TimeProvider clock, ILogger<ClosePurchaseRequestHandler> logger)
    : ICommandHandler<ClosePurchaseRequestCommand, string>
{
    public async Task<Result<string>> HandleAsync(ClosePurchaseRequestCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await RequestAccess.AuthorizeAsync(requests, currentUser, command.PurchaseRequestId, ct);
        if (access.IsFailure)
            return access.Error;
        var request = await requests.GetForUpdateAsync(command.PurchaseRequestId, expected, ct);
        if (request is null)
            return PurchaseRequestErrors.NotFound;

        var closed = request.Close(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (closed.IsFailure)
            return closed.Error;
        var saved = await requests.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        RequestClosed(logger, request.Id);
        return Convert.ToBase64String(request.RowVersion);
    }

    [LoggerMessage(EventId = 6056, Level = LogLevel.Information, Message = "Purchase request {RequestId} closed")]
    private static partial void RequestClosed(ILogger logger, Guid requestId);
}
