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

namespace WarehouseERP.Accounting.Application.Features.Journals.CreateOpeningJournal;

// accounting.opening.manage: the general-ledger balances accounting starts from, on the start date. System accounts
// (inventory, GRNI) take their opening figures here; customer and supplier balances come with their parties in
// receivables and payables. Always approved before it posts.
public sealed partial class CreateOpeningJournalHandler(
    IJournalRepository journals, IAccountingSettingsRepository settings, JournalLineRules lineRules, AccountingReferences references,
    ICurrentUser currentUser, TimeProvider clock, ILogger<CreateOpeningJournalHandler> logger)
    : ICommandHandler<CreateOpeningJournalCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateOpeningJournalCommand command, CancellationToken ct)
    {
        var access = JournalAccess.CanManage(currentUser, JournalType.OpeningBalance);
        if (access.IsFailure)
            return access.Error;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var policies = await settings.GetAsync(ct);
        if (policies.IsActivated)
            return JournalErrors.OpeningAfterActivation;
        var lines = JournalLineRules.ToInputs(command.Lines);
        var checkedLines = await lineRules.CheckAsync(lines, JournalType.OpeningBalance, ct);
        if (checkedLines.IsFailure)
            return checkedLines.Error;
        var created = JournalEntry.CreateDraft(JournalType.OpeningBalance, policies.CutoverDate, command.Description, command.Reference,
            company.Value.BaseCurrencyCode, requiresApproval: true, lines, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        journals.Add(created.Value);
        var saved = await journals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        OpeningDrafted(logger, created.Value.Id, policies.CutoverDate, created.Value.TotalDebit);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 9042, Level = LogLevel.Information, Message = "Opening-balance journal {JournalId} drafted on {CutoverDate} for {Total}")]
    private static partial void OpeningDrafted(ILogger logger, Guid journalId, DateOnly cutoverDate, decimal total);
}
