using WarehouseERP.Accounting.Application.Contracts.SourcePostings;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings.RunSourceSync;

// The worker's run, on demand (accounting.integration.manage, company-wide): find new documents, journal what is due.
public sealed class RunSourceSyncHandler(SourceSync sync, ICurrentUser currentUser) : ICommandHandler<RunSourceSyncCommand, SourceSyncResponse>
{
    public async Task<Result<SourceSyncResponse>> HandleAsync(RunSourceSyncCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return await sync.RunAsync(command.FullScan, ct);
    }
}
