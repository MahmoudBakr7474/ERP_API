using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals.RejectJournal;

// An approver sends a submitted journal back to draft with a reason.
public sealed partial class RejectJournalHandler(
    IJournalRepository journals, ICurrentUser currentUser, TimeProvider clock, ILogger<RejectJournalHandler> logger)
    : ICommandHandler<RejectJournalCommand, string>
{
    public async Task<Result<string>> HandleAsync(RejectJournalCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var journal = await journals.GetForUpdateAsync(command.JournalId, expected, ct);
        if (journal is null)
            return JournalErrors.NotFound;
        var rejected = journal.Reject(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (rejected.IsFailure)
            return rejected.Error;
        var saved = await journals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        JournalRejected(logger, journal.Id);
        return Convert.ToBase64String(journal.RowVersion);
    }

    [LoggerMessage(EventId = 9045, Level = LogLevel.Information, Message = "Journal {JournalId} rejected back to draft")]
    private static partial void JournalRejected(ILogger logger, Guid journalId);
}
