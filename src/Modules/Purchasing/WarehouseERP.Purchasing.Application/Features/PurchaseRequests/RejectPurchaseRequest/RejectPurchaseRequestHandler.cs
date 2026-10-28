using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.RejectPurchaseRequest;

// An approver sends a submitted request back to draft with a reason. Returns the new row version.
public sealed partial class RejectPurchaseRequestHandler(
    IPurchaseRequestRepository requests, ICurrentUser currentUser, TimeProvider clock, ILogger<RejectPurchaseRequestHandler> logger)
    : ICommandHandler<RejectPurchaseRequestCommand, string>
{
    public async Task<Result<string>> HandleAsync(RejectPurchaseRequestCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await RequestAccess.AuthorizeAsync(requests, currentUser, command.PurchaseRequestId, ct);
        if (access.IsFailure)
            return access.Error;
        var request = await requests.GetForUpdateAsync(command.PurchaseRequestId, expected, ct);
        if (request is null)
            return PurchaseRequestErrors.NotFound;

        var rejected = request.Reject(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (rejected.IsFailure)
            return rejected.Error;
        var saved = await requests.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        RequestRejected(logger, request.Id);
        return Convert.ToBase64String(request.RowVersion);
    }

    [LoggerMessage(EventId = 6054, Level = LogLevel.Information, Message = "Purchase request {RequestId} rejected")]
    private static partial void RequestRejected(ILogger logger, Guid requestId);
}
