namespace WarehouseERP.Accounting.Contracts;

/// <summary>
/// Permissions Accounting endpoints require. Public so the host can register policies and the Identity module can grant
/// them. Every accounting action also needs company-wide access (V1 decision D17): branch figures are report filters, not
/// security. Whoever holds a permission may use it on their own work too: an approver approves the journals they wrote.
/// </summary>
public static class AccountingPermissions
{
    /// <summary>Read the chart of accounts, the account mappings and the fiscal years with their periods.</summary>
    public const string AccountsRead = "accounting.accounts.read";

    /// <summary>Create, change, import, activate, deactivate and delete accounts, and map them to posting roles.</summary>
    public const string AccountsManage = "accounting.accounts.manage";

    /// <summary>Create fiscal years, close their periods and close a fiscal year.</summary>
    public const string PeriodsManage = "accounting.periods.manage";

    /// <summary>Reopen a closed period or a closed fiscal year, with a reason.</summary>
    public const string PeriodsReopen = "accounting.periods.reopen";

    /// <summary>Read journals with their lines and history.</summary>
    public const string JournalsRead = "accounting.journals.read";

    /// <summary>Draft, change, submit, post (when no approval is needed), cancel and reverse manual journals.</summary>
    public const string JournalsManage = "accounting.journals.manage";

    /// <summary>Approve (and so post) or reject submitted journals, one's own included.</summary>
    public const string JournalsApprove = "accounting.journals.approve";

    /// <summary>Draft and change the opening-balance journal of the accounting start date.</summary>
    public const string OpeningBalancesManage = "accounting.opening.manage";

    /// <summary>Read the trial balance, general ledger, account statements and financial statements.</summary>
    public const string ReportsRead = "accounting.reports.read";

    /// <summary>Change the accounting settings: fiscal-year start, accounting start date, approval policies.</summary>
    public const string SettingsManage = "accounting.settings.manage";

    /// <summary>Draft and change customer receipts and refunds, and allocate customers' open items.</summary>
    public const string ReceivablesManage = "accounting.receivables.manage";

    /// <summary>Draft and change supplier payments, refunds and expense bills, and allocate suppliers' open items.</summary>
    public const string PayablesManage = "accounting.payables.manage";

    /// <summary>Manage cashboxes and bank accounts, cash receipts, payments and transfers, and bank reconciliations.</summary>
    public const string TreasuryManage = "accounting.treasury.manage";

    /// <summary>Every Accounting permission.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        AccountsRead, AccountsManage, PeriodsManage, PeriodsReopen, JournalsRead, JournalsManage, JournalsApprove,
        OpeningBalancesManage, ReportsRead, SettingsManage, ReceivablesManage, PayablesManage, TreasuryManage,
    ];
}
