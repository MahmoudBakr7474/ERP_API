using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals.CreateJournal;

// A balanced manual draft in the base currency, dated on or after the accounting start date, on accounts open to manual
// journals. It copies the settings' approval policy now; later policy changes don't touch it.
public sealed partial class CreateJournalHandler(
    IJournalRepository journals, IAccountingSettingsRepository settings, JournalLineRules lineRules, AccountingReferences references,
    ICurrentUser currentUser, TimeProvider clock, ILogger<CreateJournalHandler> logger)
    : ICommandHandler<CreateJournalCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateJournalCommand command, CancellationToken ct)
    {
        var access = JournalAccess.CanManage(currentUser, JournalType.Manual);
        if (access.IsFailure)
            return access.Error;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var policies = await settings.GetAsync(ct);
        if (command.PostingDate < policies.CutoverDate)
            return JournalErrors.BeforeCutover(policies.CutoverDate);
        var lines = JournalLineRules.ToInputs(command.Lines);
        var checkedLines = await lineRules.CheckAsync(lines, JournalType.Manual, ct);
        if (checkedLines.IsFailure)
            return checkedLines.Error;
        var created = JournalEntry.CreateDraft(JournalType.Manual, command.PostingDate, command.Description, command.Reference,
            company.Value.BaseCurrencyCode, policies.ManualJournalsRequireApproval, lines, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        journals.Add(created.Value);
        var saved = await journals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        JournalDrafted(logger, created.Value.Id, created.Value.TotalDebit);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 9041, Level = LogLevel.Information, Message = "Manual journal {JournalId} drafted for {Total}")]
    private static partial void JournalDrafted(ILogger logger, Guid journalId, decimal total);
}
