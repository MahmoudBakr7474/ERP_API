namespace WarehouseERP.Accounting.Application.Features.Settings.UpdateAccountingSettings;

/// <param name="CutoverDate">The first day of a month; the owner approves it before accounting is activated.</param>
public sealed record UpdateAccountingSettingsCommand(
    int FiscalYearStartMonth, DateOnly CutoverDate, bool ManualJournalsRequireApproval, bool VouchersRequireApproval, string RowVersion);
