namespace WarehouseERP.Accounting.Application.Features.Settings.GetAccountingSettings;

/// <param name="FiscalYearStartMonth">1 (January) to 12.</param>
/// <param name="CutoverDate">The accounting start date: nothing is journaled before it; opening balances are dated on it.</param>
/// <param name="VouchersRequireApproval">Receipts, payments, refunds, cash vouchers, transfers and expense bills need an approver.</param>
/// <param name="StartMonthLocked">True once a fiscal year exists.</param>
/// <param name="CutoverLocked">True once a journal has posted or automatic accounting is active.</param>
/// <param name="ActivatedAtUtc">When automatic accounting of operational documents started; null before the cutover's activation.</param>
public sealed record AccountingSettingsResponse(
    int FiscalYearStartMonth, DateOnly CutoverDate, bool ManualJournalsRequireApproval, bool VouchersRequireApproval, string CurrencyCode,
    bool StartMonthLocked, bool CutoverLocked, string RowVersion, DateTime? ActivatedAtUtc);
