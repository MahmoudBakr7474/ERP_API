namespace WarehouseERP.Purchasing.Contracts;

/// <summary>
/// Permissions Purchasing endpoints require. Public so the host can register policies and the Identity module can grant
/// them. Suppliers are company-wide master data (no scope); requests, orders, receipts, invoices and returns follow the
/// warehouse scope of their order; changing approval policies needs company-wide access. Whoever holds the approval
/// permission (and the warehouse in scope) approves, their own documents included.
/// </summary>
public static class PurchasingPermissions
{
    /// <summary>Read suppliers, their contacts and addresses, and check names for duplicates.</summary>
    public const string SuppliersRead = "purchasing.suppliers.read";

    /// <summary>Create, change, activate and deactivate suppliers.</summary>
    public const string SuppliersManage = "purchasing.suppliers.manage";

    /// <summary>Read purchase requests, orders, goods receipts, supplier invoices, returns and the approval policies.</summary>
    public const string DocumentsRead = "purchasing.documents.read";

    /// <summary>Draft, submit, cancel and close purchase requests.</summary>
    public const string RequestsManage = "purchasing.requests.manage";

    /// <summary>Draft, submit, cancel and close purchase orders.</summary>
    public const string OrdersManage = "purchasing.orders.manage";

    /// <summary>Receive goods against approved purchase orders, which posts them to stock.</summary>
    public const string ReceiptsManage = "purchasing.receipts.manage";

    /// <summary>Record, submit, post and cancel supplier invoices.</summary>
    public const string InvoicesManage = "purchasing.invoices.manage";

    /// <summary>Draft, submit and cancel supplier returns.</summary>
    public const string ReturnsManage = "purchasing.returns.manage";

    /// <summary>
    /// Approve or reject purchase requests, orders, invoices with price differences and supplier returns, one's own
    /// included.
    /// </summary>
    public const string DocumentsApprove = "purchasing.documents.approve";

    /// <summary>Change which purchasing documents need approval (company-wide users only).</summary>
    public const string SettingsManage = "purchasing.settings.manage";

    /// <summary>Every Purchasing permission.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        SuppliersRead, SuppliersManage, DocumentsRead, RequestsManage, OrdersManage, ReceiptsManage, InvoicesManage,
        ReturnsManage, DocumentsApprove, SettingsManage,
    ];
}
