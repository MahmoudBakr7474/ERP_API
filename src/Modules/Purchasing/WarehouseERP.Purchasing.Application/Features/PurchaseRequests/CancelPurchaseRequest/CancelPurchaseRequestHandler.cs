using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.CancelPurchaseRequest;

// A request nothing was ordered from is abandoned; once ordered from, it can only be closed. Returns the new row version.
public sealed partial class CancelPurchaseRequestHandler(
    IPurchaseRequestRepository requests, ICurrentUser currentUser, TimeProvider clock, ILogger<CancelPurchaseRequestHandler> logger)
    : ICommandHandler<CancelPurchaseRequestCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelPurchaseRequestCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await RequestAccess.AuthorizeAsync(requests, currentUser, command.PurchaseRequestId, ct);
        if (access.IsFailure)
            return access.Error;
        var request = await requests.GetForUpdateAsync(command.PurchaseRequestId, expected, ct);
        if (request is null)
            return PurchaseRequestErrors.NotFound;

        var cancelled = request.Cancel(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (cancelled.IsFailure)
            return cancelled.Error;
        var saved = await requests.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        RequestCancelled(logger, request.Id);
        return Convert.ToBase64String(request.RowVersion);
    }

    [LoggerMessage(EventId = 6055, Level = LogLevel.Information, Message = "Purchase request {RequestId} cancelled")]
    private static partial void RequestCancelled(ILogger logger, Guid requestId);
}
