using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals.UpdateJournal;

// Drafts only, from the version the editor read. Opening balances keep the start date (and take a changed one); a
// reversal keeps its mirrored lines and stays dated on or after the journal it reverses.
public sealed partial class UpdateJournalHandler(
    IJournalRepository journals, IAccountingSettingsRepository settings, JournalLineRules lineRules, ICurrentUser currentUser,
    TimeProvider clock, ILogger<UpdateJournalHandler> logger)
    : ICommandHandler<UpdateJournalCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateJournalCommand command, CancellationToken ct)
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

        var policies = await settings.GetAsync(ct);
        var postingDate = journal.JournalType == JournalType.OpeningBalance ? policies.CutoverDate : command.PostingDate;
        if (postingDate < policies.CutoverDate)
            return JournalErrors.BeforeCutover(policies.CutoverDate);
        if (journal.ReversalOfJournalId is { } reversedId
            && await journals.FindAsync(reversedId, ct) is { } reversed && postingDate < reversed.PostingDate)
            return JournalErrors.ReversalBeforeOriginal;
        var lines = command.Lines is null ? null : JournalLineRules.ToInputs(command.Lines);
        if (lines is not null && !journal.LinesFixed)
        {
            var checkedLines = await lineRules.CheckAsync(lines, journal.JournalType, ct);
            if (checkedLines.IsFailure)
                return checkedLines.Error;
        }
        var replaced = journal.ReplaceDraft(postingDate, command.Description, command.Reference, lines, currentUser.UserId,
            clock.GetUtcNow().UtcDateTime);
        if (replaced.IsFailure)
            return replaced.Error;
        var saved = await journals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        JournalUpdated(logger, journal.Id, journal.TotalDebit);
        return Convert.ToBase64String(journal.RowVersion);
    }

    [LoggerMessage(EventId = 9043, Level = LogLevel.Information, Message = "Journal {JournalId} draft updated, total {Total}")]
    private static partial void JournalUpdated(ILogger logger, Guid journalId, decimal total);
}
