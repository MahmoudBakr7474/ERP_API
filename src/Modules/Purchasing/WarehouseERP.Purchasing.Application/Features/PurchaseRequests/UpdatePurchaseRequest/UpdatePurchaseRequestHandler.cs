using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.UpdatePurchaseRequest;

// Only a draft changes (nothing can be on order from it yet), only from the version its editor read. Returns the new row version.
public sealed partial class UpdatePurchaseRequestHandler(
    IPurchaseRequestRepository requests, PurchasingReferences references, ICurrentUser currentUser,
    ILogger<UpdatePurchaseRequestHandler> logger)
    : ICommandHandler<UpdatePurchaseRequestCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdatePurchaseRequestCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await RequestAccess.AuthorizeAsync(requests, currentUser, command.PurchaseRequestId, ct);
        if (access.IsFailure)
            return access.Error;
        var request = await requests.GetForUpdateAsync(command.PurchaseRequestId, expected, ct);
        if (request is null)
            return PurchaseRequestErrors.NotFound;
        var lines = await RequestLines.ResolveAsync(references, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;

        var replaced = request.ReplaceDraft(command.NeededByDate, command.Notes, lines.Value);
        if (replaced.IsFailure)
            return replaced.Error;
        var saved = await requests.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        RequestUpdated(logger, request.Id);
        return Convert.ToBase64String(request.RowVersion);
    }

    [LoggerMessage(EventId = 6051, Level = LogLevel.Information, Message = "Purchase request {RequestId} draft updated")]
    private static partial void RequestUpdated(ILogger logger, Guid requestId);
}
