using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Settings.UpdateAccountingSettings;

// Company-wide users with accounting.settings.manage (the endpoint), from the version the editor read. The fiscal-year start
// month is fixed once a year exists and the start date once a journal has posted; the approval policy applies to journals
// drafted from now on.
public sealed partial class UpdateAccountingSettingsHandler(
    IAccountingSettingsRepository settings, IFiscalYearRepository years, IJournalRepository journals, ICurrentUser currentUser,
    ILogger<UpdateAccountingSettingsHandler> logger)
    : ICommandHandler<UpdateAccountingSettingsCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateAccountingSettingsCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var current = await settings.GetForUpdateAsync(expected, ct);
        if (command.FiscalYearStartMonth != current.FiscalYearStartMonth && await years.AnyAsync(ct))
            return SettingsErrors.StartMonthLocked;
        if (command.CutoverDate != current.CutoverDate && await journals.AnyPostedAsync(ct))
            return SettingsErrors.CutoverLocked;
        var changed = current.Change(command.FiscalYearStartMonth, command.CutoverDate, command.ManualJournalsRequireApproval,
            command.VouchersRequireApproval);
        if (changed.IsFailure)
            return changed.Error;
        var saved = await settings.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        SettingsChanged(logger, command.FiscalYearStartMonth, command.CutoverDate, command.ManualJournalsRequireApproval);
        return Convert.ToBase64String(current.RowVersion);
    }

    [LoggerMessage(EventId = 9001, Level = LogLevel.Information,
        Message = "Accounting settings changed; fiscal years start in month {StartMonth}, accounting starts {CutoverDate}, manual journals need approval: {RequireApproval}")]
    private static partial void SettingsChanged(ILogger logger, int startMonth, DateOnly cutoverDate, bool requireApproval);
}
