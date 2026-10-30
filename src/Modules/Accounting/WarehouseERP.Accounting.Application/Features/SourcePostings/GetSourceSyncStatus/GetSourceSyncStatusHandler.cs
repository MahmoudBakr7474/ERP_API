using Microsoft.Extensions.Options;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceSyncStatus;

public sealed class GetSourceSyncStatusHandler(
    ISourcePostingReadStore readStore, IAccountingSettingsRepository settings, IOptions<AccountingIntegrationOptions> options,
    ICurrentUser currentUser)
    : IQueryHandler<GetSourceSyncStatusQuery, SourceSyncStatusResponse>
{
    public async Task<Result<SourceSyncStatusResponse>> HandleAsync(GetSourceSyncStatusQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var policies = await settings.GetAsync(ct);
        var counts = await readStore.CountsAsync(ct);
        int Count(SourcePostingStatus status) => counts.Where(c => c.Status == status).Sum(c => c.Count);
        return new SourceSyncStatusResponse(
            policies.IsActivated, policies.ActivatedAtUtc, policies.CutoverDate, options.Value.WorkerEnabled,
            (int)options.Value.Interval.TotalSeconds, Count(SourcePostingStatus.Pending), Count(SourcePostingStatus.Failed),
            Count(SourcePostingStatus.Posted), Count(SourcePostingStatus.NothingToPost), await readStore.OldestUnsettledAsync(ct),
            await readStore.CursorsAsync(ct));
    }
}
