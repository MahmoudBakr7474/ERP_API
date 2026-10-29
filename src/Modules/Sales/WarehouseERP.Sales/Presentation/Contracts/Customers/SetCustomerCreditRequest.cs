namespace WarehouseERP.Sales.Presentation.Contracts.Customers;

/// <summary>The customer's credit terms and manual hold.</summary>
/// <param name="CreditLimit">Null for none; informational until Accounting tracks receivables.</param>
/// <param name="CreditHoldReason">Required while on hold.</param>
public sealed record SetCustomerCreditRequest(
    int PaymentTermDays, decimal? CreditLimit, bool IsOnCreditHold, string? CreditHoldReason, string RowVersion);
