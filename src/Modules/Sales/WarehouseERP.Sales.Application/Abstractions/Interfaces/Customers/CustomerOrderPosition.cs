namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;

/// <summary>Amounts (net plus tax, base currency) over the customer's confirmed, delivered and closed orders.</summary>
/// <param name="UninvoicedOrderAmount">What committed orders hold that is not invoiced yet, delivered or not (closed and returned-before-invoicing parts excluded).</param>
/// <param name="DeliveredNotInvoicedAmount">The part of it already delivered.</param>
/// <param name="InvoicedAmount">Posted invoices; payments are not tracked yet.</param>
/// <param name="PendingCreditAmount">Credits owed to the customer for invoiced goods that came back.</param>
public sealed record CustomerOrderPosition(
    int OpenOrderCount, decimal UninvoicedOrderAmount, decimal DeliveredNotInvoicedAmount, decimal InvoicedAmount,
    decimal PendingCreditAmount);
