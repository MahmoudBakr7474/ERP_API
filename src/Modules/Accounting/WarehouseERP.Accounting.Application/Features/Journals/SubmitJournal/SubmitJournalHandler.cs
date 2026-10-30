using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals.SubmitJournal;

// A draft that needs approval goes to the approvers; one that doesn't is posted directly instead.
public sealed partial class SubmitJournalHandler(
    IJournalRepository journals, ICurrentUser currentUser, TimeProvider clock, ILogger<SubmitJournalHandler> logger)
    : ICommandHandler<SubmitJournalCommand, string>
{
    public async Task<Result<string>> HandleAsync(SubmitJournalCommand command, CancellationToken ct)
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
        var submitted = journal.Submit(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (submitted.IsFailure)
            return submitted.Error;
        var saved = await journals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        JournalSubmitted(logger, journal.Id);
        return Convert.ToBase64String(journal.RowVersion);
    }

    [LoggerMessage(EventId = 9044, Level = LogLevel.Information, Message = "Journal {JournalId} submitted for approval")]
    private static partial void JournalSubmitted(ILogger logger, Guid journalId);
}
