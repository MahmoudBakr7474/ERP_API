using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Branches.UpdateBranch;

// Company-wide users only; the code is fixed. Returns the new row version for the caller's next update.
public sealed partial class UpdateBranchHandler(
    IBranchRepository branches, ICurrentUser currentUser, ILogger<UpdateBranchHandler> logger)
    : ICommandHandler<UpdateBranchCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateBranchCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var branch = await branches.GetForUpdateAsync(command.BranchId, expected, ct);
        if (branch is null)
            return BranchErrors.NotFound;

        branch.Rename(command.NameAr, command.NameEn);
        var saved = await branches.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        BranchRenamed(logger, branch.Id);
        return Convert.ToBase64String(branch.RowVersion);
    }

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Branch {BranchId} renamed")]
    private static partial void BranchRenamed(ILogger logger, Guid branchId);
}
