namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;

/// <param name="PaymentTermDays">Days from invoice date to due date that new quotations and orders start with.</param>
public sealed record CustomerState(
    Guid Id, string Code, string NameAr, bool IsActive, int PaymentTermDays, decimal? CreditLimit, bool IsOnCreditHold);
