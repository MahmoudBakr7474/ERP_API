namespace WarehouseERP.Pos.Contracts;

/// <summary>
/// Permissions POS endpoints require. Public so the host can register policies and the Identity module can grant them.
/// Terminals, shifts, sales and returns follow the warehouse scope of their terminal; POS settings need company-wide
/// access. Whoever holds a permission (and the warehouse in scope) may use it on their own work too: a cashier may
/// discount, refund or reprint their own sales when they hold those permissions.
/// </summary>
public static class PosPermissions
{
    /// <summary>Read terminals, shifts with their cash, sales, returns, receipts and card incidents.</summary>
    public const string Read = "pos.read";

    /// <summary>Open and close one's own shift, look up items and customers, and check out sales on it.</summary>
    public const string Sell = "pos.sell";

    /// <summary>Give line discounts, up to the maximum in the POS settings.</summary>
    public const string DiscountsApply = "pos.discounts.apply";

    /// <summary>Record cash put into or taken out of a shift's drawer.</summary>
    public const string CashManage = "pos.cash.manage";

    /// <summary>Take goods back against a POS sale and refund them, one's own sales included.</summary>
    public const string ReturnsManage = "pos.returns.manage";

    /// <summary>Print copies of receipts (each copy is counted and marked as a copy).</summary>
    public const string ReceiptsReprint = "pos.receipts.reprint";

    /// <summary>Close another cashier's abandoned shift, record its cash, and resolve card incidents.</summary>
    public const string ShiftsManage = "pos.shifts.manage";

    /// <summary>Register terminals and change their settings.</summary>
    public const string TerminalsManage = "pos.terminals.manage";

    /// <summary>Change the POS settings: maximum cashier discount and return window (company-wide users only).</summary>
    public const string SettingsManage = "pos.settings.manage";

    /// <summary>Every POS permission.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Read, Sell, DiscountsApply, CashManage, ReturnsManage, ReceiptsReprint, ShiftsManage, TerminalsManage, SettingsManage,
    ];
}
