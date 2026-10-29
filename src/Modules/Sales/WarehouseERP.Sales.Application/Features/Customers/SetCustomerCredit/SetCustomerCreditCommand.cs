namespace WarehouseERP.Sales.Application.Features.Customers.SetCustomerCredit;

/// <summary>The customer's credit terms and manual hold.</summary>
/// <param name="PaymentTermDays">Days from invoice date to due date new documents start with, 0 (cash) to 365.</param>
/// <param name="CreditLimit">In the base currency; null for none. Informational until Accounting tracks receivables.</param>
/// <param name="IsOnCreditHold">What a hold blocks depends on the sales settings.</param>
/// <param name="CreditHoldReason">Required while on hold.</param>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record SetCustomerCreditCommand(
    Guid CustomerId, int PaymentTermDays, decimal? CreditLimit, bool IsOnCreditHold, string? CreditHoldReason, string RowVersion);
