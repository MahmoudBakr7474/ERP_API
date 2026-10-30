namespace WarehouseERP.Sales.Application.Features.Customers.GetCustomerExposure;

/// <summary>
/// The customer's credit exposure: informational (nothing is refused because of it). Until Accounting is active it is
/// partial, Sales' own sales-order view; once it is, the receivable balance (posted invoices less receipts and credits,
/// opening balances included), plus invoices and credits posted but not journaled yet, plus confirmed orders not invoiced
/// yet is the total exposure, each amount counted once: an invoiced order is in the receivable (or waiting for its journal),
/// not in the uninvoiced amount.
/// </summary>
/// <param name="IsPartial">True until Accounting is active: payments are not tracked, so this is not the outstanding balance.</param>
/// <param name="CreditLimit">The limit set on the customer, for comparison only; null when none.</param>
/// <param name="UninvoicedOrderAmount">Confirmed orders not invoiced yet, delivered or not (net plus tax).</param>
/// <param name="DeliveredNotInvoicedAmount">The part of it already delivered.</param>
/// <param name="InvoicedAmount">Posted invoices, paid or not.</param>
/// <param name="PendingCreditAmount">Credits Sales recorded for invoiced goods that came back (in the receivable once Accounting is active).</param>
/// <param name="ReceivableBalance">What the customer owes on the receivable ledger; null until Accounting is active.</param>
/// <param name="UnjournaledAmount">Invoices less credits posted since the cutover whose journal hasn't posted yet; null until Accounting is active.</param>
/// <param name="TotalExposure">Receivable balance plus the unjournaled amount plus uninvoiced orders; null until Accounting is active.</param>
/// <param name="AvailableCredit">Credit limit less total exposure (negative when over); null without a limit or before Accounting.</param>
public sealed record CustomerExposureResponse(
    Guid CustomerId, string CurrencyCode, bool IsPartial, decimal? CreditLimit, bool IsOnCreditHold, int PaymentTermDays,
    int OpenOrderCount, decimal UninvoicedOrderAmount, decimal DeliveredNotInvoicedAmount, decimal InvoicedAmount,
    decimal PendingCreditAmount, decimal? ReceivableBalance, decimal? TotalExposure, decimal? AvailableCredit, decimal? UnjournaledAmount);
