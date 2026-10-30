using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals.CancelJournal;

// A draft or submitted journal is dropped (it keeps its draft reference and history); a posted one is reversed instead.
// Cancelling a reversal frees its journal for another reversal.
public sealed partial class CancelJournalHandler(
    IJournalRepository journals, ICurrentUser currentUser, TimeProvider clock, ILogger<CancelJournalHandler> logger)
    : ICommandHandler<CancelJournalCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelJournalCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var journal = await journals.GetForUpdateAsync(command.JournalId, expected, ct);
        if (journal is null)
        {
            var denied = JournalAccess.CanManage(currentUser, JournalType.Manual);
            return denied.IsFailure ? denied.Error : JournalErrors.NotFound;
        }
        var access = JournalAccess.CanManage(currentUser, journal.JournalType);
        if (access.IsFailure)
            return access.Error;
        var cancelled = journal.Cancel(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (cancelled.IsFailure)
            return cancelled.Error;
        var saved = await journals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        JournalCancelled(logger, journal.Id);
        return Convert.ToBase64String(journal.RowVersion);
    }

    [LoggerMessage(EventId = 9046, Level = LogLevel.Information, Message = "Journal {JournalId} cancelled")]
    private static partial void JournalCancelled(ILogger logger, Guid journalId);
}
