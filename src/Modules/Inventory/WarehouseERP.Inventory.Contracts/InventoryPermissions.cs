namespace WarehouseERP.Inventory.Contracts;

/// <summary>
/// Permissions Inventory endpoints require. Public so the host can register policies and the Identity module can
/// grant them. Stock and documents follow the user's warehouse scope; valuation is company-wide data; changing
/// document policies needs company-wide access.
/// </summary>
public static class InventoryPermissions
{
    /// <summary>Read stock balances, availability, the movement ledger and over-reservation alerts.</summary>
    public const string StockRead = "inventory.stock.read";

    /// <summary>Read stock documents and the document policies.</summary>
    public const string DocumentsRead = "inventory.documents.read";

    /// <summary>Draft, submit, post, cancel and reverse opening stock documents.</summary>
    public const string OpeningManage = "inventory.opening.manage";

    /// <summary>Draft, submit, post, cancel and reverse goods receipts.</summary>
    public const string ReceiptsManage = "inventory.receipts.manage";

    /// <summary>Draft, submit, post, cancel and reverse goods issues.</summary>
    public const string IssuesManage = "inventory.issues.manage";

    /// <summary>Draft, submit, post, cancel and reverse stock adjustments.</summary>
    public const string AdjustmentsManage = "inventory.adjustments.manage";

    /// <summary>Approve (which posts) or reject submitted documents; nobody approves a document they created.</summary>
    public const string DocumentsApprove = "inventory.documents.approve";

    /// <summary>Read the company-wide valuation (moving average cost) and its history.</summary>
    public const string ValuationRead = "inventory.valuation.read";

    /// <summary>Change which document types need approval (company-wide users only).</summary>
    public const string SettingsManage = "inventory.settings.manage";

    /// <summary>Every Inventory permission.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        StockRead, DocumentsRead, OpeningManage, ReceiptsManage, IssuesManage, AdjustmentsManage, DocumentsApprove,
        ValuationRead, SettingsManage,
    ];
}
