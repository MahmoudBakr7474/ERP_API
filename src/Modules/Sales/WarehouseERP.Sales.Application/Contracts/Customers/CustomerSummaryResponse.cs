using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Customers;

public sealed record CustomerSummaryResponse(
    Guid Id, string Code, CustomerType CustomerType, string NameAr, string? NameEn, string CountryCode, string? TaxRegistrationNumber,
    string? Mobile, string? Phone, int PaymentTermDays, decimal? CreditLimit, bool IsOnCreditHold, bool IsActive);
