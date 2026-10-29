namespace WarehouseERP.Sales.Contracts;

/// <summary>
/// Permissions Sales endpoints require. Public so the host can register policies and the Identity module can grant them.
/// Customers and the price list are company-wide master data (no scope); quotations, orders, deliveries, invoices and
/// returns follow the warehouse scope of their document; changing sales settings needs company-wide access. Whoever holds
/// the approval permission (and the warehouse in scope) approves, their own documents included.
/// </summary>
public static class SalesPermissions
{
    /// <summary>Read customers, their contacts, addresses, credit terms and order exposure, and check for duplicates.</summary>
    public const string CustomersRead = "sales.customers.read";

    /// <summary>Create, change, activate and deactivate customers.</summary>
    public const string CustomersManage = "sales.customers.manage";

    /// <summary>Change a customer's payment terms, credit limit and credit hold.</summary>
    public const string CreditManage = "sales.credit.manage";

    /// <summary>
    /// Maintain the sales price list, and set a manual selling price on a quotation or order line (where the list has no
    /// price, or instead of it).
    /// </summary>
    public const string PricesManage = "sales.prices.manage";

    /// <summary>Read quotations, orders, deliveries, invoices, returns, the price list and the sales settings.</summary>
    public const string DocumentsRead = "sales.documents.read";

    /// <summary>Draft, submit, issue, decline, cancel and convert quotations.</summary>
    public const string QuotationsManage = "sales.quotations.manage";

    /// <summary>Draft, submit, cancel, reduce and close sales orders.</summary>
    public const string OrdersManage = "sales.orders.manage";

    /// <summary>Draft picking lists and post deliveries, which take stock out of the order's reservation.</summary>
    public const string DeliveriesManage = "sales.deliveries.manage";

    /// <summary>Draft, post and cancel sales invoices.</summary>
    public const string InvoicesManage = "sales.invoices.manage";

    /// <summary>Draft, submit and cancel customer returns.</summary>
    public const string ReturnsManage = "sales.returns.manage";

    /// <summary>Approve or reject quotations, orders and customer returns, one's own included.</summary>
    public const string DocumentsApprove = "sales.documents.approve";

    /// <summary>Change the sales settings: approval policies and thresholds, credit hold policy, default VAT (company-wide users only).</summary>
    public const string SettingsManage = "sales.settings.manage";

    /// <summary>Every Sales permission.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        CustomersRead, CustomersManage, CreditManage, PricesManage, DocumentsRead, QuotationsManage, OrdersManage,
        DeliveriesManage, InvoicesManage, ReturnsManage, DocumentsApprove, SettingsManage,
    ];
}
