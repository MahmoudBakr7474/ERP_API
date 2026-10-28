using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.DocumentPolicies.UpdateDocumentPolicy;

// A company-wide rule, so only a company-wide user changes it. Documents already created keep the policy they copied.
public sealed partial class UpdateDocumentPolicyHandler(
    IDocumentPolicyRepository policies, ICurrentUser currentUser, ILogger<UpdateDocumentPolicyHandler> logger)
    : ICommandHandler<UpdateDocumentPolicyCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateDocumentPolicyCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return DocumentPolicyErrors.CompanyWideAccessRequired;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
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

    [LoggerMessage(EventId = 5007, Level = LogLevel.Information, Message = "Approval policy of {DocumentType} set to {RequiresApproval}")]
    private static partial void PolicyChanged(ILogger logger, StockDocumentType documentType, bool requiresApproval);
}
