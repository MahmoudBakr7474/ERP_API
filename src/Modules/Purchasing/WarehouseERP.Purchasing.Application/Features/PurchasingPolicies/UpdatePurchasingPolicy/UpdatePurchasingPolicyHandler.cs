using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchasingPolicies.UpdatePurchasingPolicy;

// Whether new purchase requests or orders need approval; documents already created keep the policy they copied. Only a
// company-wide user changes it. Who may approve is not a policy: anyone with the permission and the warehouse in scope.
public sealed partial class UpdatePurchasingPolicyHandler(
    IDocumentPolicyRepository policies, ICurrentUser currentUser, ILogger<UpdatePurchasingPolicyHandler> logger)
    : ICommandHandler<UpdatePurchasingPolicyCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdatePurchasingPolicyCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        if (!currentUser.HasAllWarehouses)
            return AccessErrors.CompanyWideRequired;
        if (command.DocumentType is not (PurchasingDocumentType.PurchaseRequest or PurchasingDocumentType.PurchaseOrder))
            return DocumentPolicyErrors.NotFound;
        var policy = await policies.GetForUpdateAsync(command.DocumentType, expected, ct);
        if (policy is null)
            return DocumentPolicyErrors.NotFound;

        policy.Change(command.RequiresApproval);
        var saved = await policies.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        PolicyChanged(logger, command.DocumentType, command.RequiresApproval);
        return Convert.ToBase64String(policy.RowVersion);
    }

    [LoggerMessage(EventId = 6005, Level = LogLevel.Information,
        Message = "Purchasing approval policy of {DocumentType} set to {RequiresApproval}")]
    private static partial void PolicyChanged(ILogger logger, PurchasingDocumentType documentType, bool requiresApproval);
}
