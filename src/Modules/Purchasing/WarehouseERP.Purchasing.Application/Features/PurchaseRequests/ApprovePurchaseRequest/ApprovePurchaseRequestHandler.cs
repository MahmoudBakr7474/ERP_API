using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ApprovePurchaseRequest;

// An approver (endpoint permission, warehouse scope), its creator included, approves a submitted request: it is numbered
// in the same transaction and orders may draw on it. Returns the new row version.
public sealed partial class ApprovePurchaseRequestHandler(
    ITransactionRunner transactions, IPurchaseRequestRepository requests, DocumentNumbering numbering, ICurrentUser currentUser,
    TimeProvider clock, ILogger<ApprovePurchaseRequestHandler> logger)
    : ICommandHandler<ApprovePurchaseRequestCommand, string>
{
    public async Task<Result<string>> HandleAsync(ApprovePurchaseRequestCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await RequestAccess.AuthorizeAsync(requests, currentUser, command.PurchaseRequestId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => ApproveAsync(command.PurchaseRequestId, expected, token), ct);
    }

    private async Task<Result<string>> ApproveAsync(Guid requestId, byte[] expected, CancellationToken ct)
    {
        var request = await requests.GetForUpdateAsync(requestId, expected, ct);
        if (request is null)
            return PurchaseRequestErrors.NotFound;
        var allowed = request.CanApprove();
        if (allowed.IsFailure)
            return allowed.Error;
        var number = await numbering.NextAsync(PurchasingDocumentType.PurchaseRequest, request.BranchId, ct);
        if (number.IsFailure)
            return number.Error;

        var approved = request.Approve(number.Value, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (approved.IsFailure)
            return approved.Error;
        var saved = await requests.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        RequestApproved(logger, request.Id, number.Value.Number);
        return Convert.ToBase64String(request.RowVersion);
    }

    [LoggerMessage(EventId = 6053, Level = LogLevel.Information, Message = "Purchase request {RequestId} approved as {Number}")]
    private static partial void RequestApproved(ILogger logger, Guid requestId, string number);
}
