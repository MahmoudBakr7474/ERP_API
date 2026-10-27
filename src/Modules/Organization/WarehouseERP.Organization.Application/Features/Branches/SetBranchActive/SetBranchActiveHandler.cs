using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Branches.SetBranchActive;

// Company-wide users only. A branch is deactivated after its warehouses, so an inactive branch never holds an
// active warehouse (SetWarehouseActive refuses the other direction).
public sealed partial class SetBranchActiveHandler(
    IBranchRepository branches, ICurrentUser currentUser, ILogger<SetBranchActiveHandler> logger)
    : ICommandHandler<SetBranchActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetBranchActiveCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        var branch = await branches.GetForUpdateAsync(command.BranchId, expectedRowVersion: null, ct);
        if (branch is null)
            return BranchErrors.NotFound;
        if (!command.IsActive && await branches.HasActiveWarehousesAsync(branch.Id, ct))
            return BranchErrors.HasActiveWarehouses;

        if (command.IsActive) branch.Activate();
        else branch.Deactivate();
        var saved = await branches.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ActiveChanged(logger, branch.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 2006, Level = LogLevel.Information, Message = "Branch {BranchId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid branchId, bool isActive);
}
