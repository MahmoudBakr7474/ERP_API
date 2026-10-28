using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.CreatePurchaseRequest;

// A draft request for one physical warehouse in the user's scope. It copies the request approval policy now. One save point.
public sealed partial class CreatePurchaseRequestHandler(
    IPurchaseRequestRepository requests, IDocumentPolicyRepository policies, PurchasingReferences references, ICurrentUser currentUser,
    TimeProvider clock, ILogger<CreatePurchaseRequestHandler> logger)
    : ICommandHandler<CreatePurchaseRequestCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreatePurchaseRequestCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return AccessErrors.WarehouseDenied;
        var warehouse = await references.UsableWarehouseAsync(command.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        var lines = await RequestLines.ResolveAsync(references, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;

        var requiresApproval = await policies.RequiresApprovalAsync(PurchasingDocumentType.PurchaseRequest, ct);
        var created = PurchaseRequest.CreateDraft(command.WarehouseId, warehouse.Value.BranchId, requiresApproval, command.NeededByDate,
            command.Notes, lines.Value, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        requests.Add(created.Value);
        var saved = await requests.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        RequestCreated(logger, created.Value.Id, command.WarehouseId);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 6050, Level = LogLevel.Information, Message = "Purchase request {RequestId} drafted for warehouse {WarehouseId}")]
    private static partial void RequestCreated(ILogger logger, Guid requestId, Guid warehouseId);
}
