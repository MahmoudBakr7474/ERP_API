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
/// <item>When automatic accounting was activated (after the cutover's start, once its checks pass), by whom, and the cutover's
/// start it was checked against (local midnight of the start date, kept so a later time-zone change moves nothing); never
/// undone, and the start date stays fixed from then on.</item>
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
    public DateTime? ActivatedAtUtc { get; private set; }
    public Guid? ActivatedByUserId { get; private set; }

    /// <summary>
    /// The moment documents start to be journaled automatically: everything posted before it belongs to the opening balances.
    /// </summary>
    public DateTime? CutoverStartUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>True once operational documents are journaled automatically.</summary>
    public bool IsActivated => ActivatedAtUtc is not null;

    /// <summary>Starts automatic accounting from the cutover's start, once it has passed; the caller checked the cutover first.</summary>
    public Result Activate(Guid? userId, DateTime nowUtc, DateTime cutoverStartUtc)
    {
        if (IsActivated)
            return AccountingSettingsDomainErrors.AlreadyActivated;
        if (nowUtc < cutoverStartUtc)
            return AccountingSettingsDomainErrors.CutoverNotStarted;
        ActivatedAtUtc = nowUtc;
        ActivatedByUserId = userId;
        CutoverStartUtc = cutoverStartUtc;
        return Result.Success();
    }

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
