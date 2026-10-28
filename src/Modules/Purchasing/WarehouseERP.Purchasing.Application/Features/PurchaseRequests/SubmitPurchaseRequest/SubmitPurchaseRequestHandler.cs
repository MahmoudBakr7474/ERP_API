using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.SubmitPurchaseRequest;

// A draft goes to the approvers when the policy it copied needs approval; otherwise it is approved and numbered at once,
// in one transaction with its number. Returns the new row version.
public sealed partial class SubmitPurchaseRequestHandler(
    ITransactionRunner transactions, IPurchaseRequestRepository requests, DocumentNumbering numbering, ICurrentUser currentUser,
    TimeProvider clock, ILogger<SubmitPurchaseRequestHandler> logger)
    : ICommandHandler<SubmitPurchaseRequestCommand, string>
{
    public async Task<Result<string>> HandleAsync(SubmitPurchaseRequestCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await RequestAccess.AuthorizeAsync(requests, currentUser, command.PurchaseRequestId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => SubmitAsync(command.PurchaseRequestId, expected, token), ct);
    }

    private async Task<Result<string>> SubmitAsync(Guid requestId, byte[] expected, CancellationToken ct)
    {
        var request = await requests.GetForUpdateAsync(requestId, expected, ct);
        if (request is null)
            return PurchaseRequestErrors.NotFound;
        if (request.Status != PurchaseRequestStatus.Draft)
            return PurchaseRequestDomainErrors.NotDraft;

        var now = clock.GetUtcNow().UtcDateTime;
        Result done;
        if (request.RequiresApproval)
            done = request.Submit(currentUser.UserId, now);
        else
        {
            var number = await numbering.NextAsync(PurchasingDocumentType.PurchaseRequest, request.BranchId, ct);
            if (number.IsFailure)
                return number.Error;
            done = request.ApproveDirectly(number.Value, currentUser.UserId, now);
        }
        if (done.IsFailure)
            return done.Error;
        var saved = await requests.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        RequestSubmitted(logger, request.Id, request.Status);
        return Convert.ToBase64String(request.RowVersion);
    }

    [LoggerMessage(EventId = 6052, Level = LogLevel.Information, Message = "Purchase request {RequestId} submitted: now {Status}")]
    private static partial void RequestSubmitted(ILogger logger, Guid requestId, PurchaseRequestStatus status);
}
