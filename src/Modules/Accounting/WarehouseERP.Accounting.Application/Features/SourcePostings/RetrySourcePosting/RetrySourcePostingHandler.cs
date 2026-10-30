using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;
using WarehouseERP.Accounting.Application.Contracts.SourcePostings;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings.RetrySourcePosting;

// Tries one document now, whatever its retry time (after the cause is fixed: a role mapped, a period reopened, a POS
// cashbox marked). A document already journaled is answered as it is.
public sealed class RetrySourcePostingHandler(
    SourcePoster poster, ISourcePostingReadStore readStore, IAccountingSettingsRepository settings, ICurrentUser currentUser)
    : ICommandHandler<RetrySourcePostingCommand, SourcePostingResponse>
{
    public async Task<Result<SourcePostingResponse>> HandleAsync(RetrySourcePostingCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (!(await settings.GetAsync(ct)).IsActivated)
            return SourcePostingErrors.NotActive;
        var record = await readStore.FindAsync(command.SourcePostingId, ct);
        if (record is null)
            return SourcePostingErrors.NotFound;
        if (record.Status is WarehouseERP.Accounting.Contracts.SourcePostingStatus.Posted or WarehouseERP.Accounting.Contracts.SourcePostingStatus.NothingToPost)
            return record;
        await poster.PostAsync(command.SourcePostingId, ct);
        return await readStore.FindAsync(command.SourcePostingId, ct) ?? throw new InvalidOperationException(
            $"Source posting {command.SourcePostingId} disappeared while it was retried.");
    }
}
