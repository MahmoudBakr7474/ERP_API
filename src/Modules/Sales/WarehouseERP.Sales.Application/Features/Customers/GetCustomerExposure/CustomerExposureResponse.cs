namespace WarehouseERP.Sales.Application.Features.Customers.GetCustomerExposure;

/// <summary>
/// The customer's sales-order exposure as Sales knows it: informational and partial. It is not total credit utilization:
/// customer payments and receivable balances are not tracked until Accounting, so nothing is refused because of it.
/// </summary>
/// <param name="IsPartial">Always true in V1: payments are not tracked, so this is not the customer's outstanding balance.</param>
/// <param name="CreditLimit">The limit set on the customer, for comparison only; null when none.</param>
/// <param name="UninvoicedOrderAmount">Confirmed orders not invoiced yet, delivered or not (net plus tax): the partial exposure.</param>
/// <param name="DeliveredNotInvoicedAmount">The part of it already delivered.</param>
/// <param name="InvoicedAmount">Posted invoices; whether they were paid is unknown in V1.</param>
/// <param name="PendingCreditAmount">Credits owed to the customer for invoiced goods that came back.</param>
public sealed record CustomerExposureResponse(
    Guid CustomerId, string CurrencyCode, bool IsPartial, decimal? CreditLimit, bool IsOnCreditHold, int PaymentTermDays,
    int OpenOrderCount, decimal UninvoicedOrderAmount, decimal DeliveredNotInvoicedAmount, decimal InvoicedAmount,
    decimal PendingCreditAmount);
