using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// The company's accounting policies: one row, edited by company-wide users, versioned in a temporal table.
/// <list type="bullet">
/// <item>The month fiscal years start in (seeded January); fixed once a fiscal year exists.</item>
/// <item>The accounting start (cutover) date (seeded 2027-01-01, the owner's proposed default): the first day of a month;
/// nothing is journaled before it, and its opening balances are dated on it. Fixed once a journal has posted.</item>
/// <item>Whether manual journals need an approver (seeded yes); a journal copies it when it is drafted.</item>
/// <item>Whether vouchers (receipts, payments, refunds, cash vouchers, transfers, expense bills) need one (seeded no).</item>
/// </list>
/// </summary>
public sealed class AccountingSettings : IAuditable
{
    public const int SingletonId = 1;

    private AccountingSettings() { } // EF Core

    public int Id { get; private set; }
    public int FiscalYearStartMonth { get; private set; }
    public DateOnly CutoverDate { get; private set; }
    public bool ManualJournalsRequireApproval { get; private set; }
    public bool VouchersRequireApproval { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public Result Change(int fiscalYearStartMonth, DateOnly cutoverDate, bool manualJournalsRequireApproval, bool vouchersRequireApproval)
    {
        if (fiscalYearStartMonth is < 1 or > 12)
            return AccountingSettingsDomainErrors.StartMonthInvalid;
        if (cutoverDate.Day != 1)
            return AccountingSettingsDomainErrors.CutoverNotPeriodStart;
        FiscalYearStartMonth = fiscalYearStartMonth;
        CutoverDate = cutoverDate;
        ManualJournalsRequireApproval = manualJournalsRequireApproval;
        VouchersRequireApproval = vouchersRequireApproval;
        return Result.Success();
    }
}
