using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Customers;

/// <param name="PaymentTermDays">Days from invoice date to due date new documents start with; 0 is cash.</param>
/// <param name="CreditLimit">Informational until Accounting tracks receivables; null when none is set.</param>
/// <param name="CreditChangedBy">Who last changed the credit terms or the hold.</param>
/// <param name="RowVersion">Send it back with the next change.</param>
public sealed record CustomerDetailsResponse(
    Guid Id, string Code, CustomerType CustomerType, string NameAr, string? NameEn, string CountryCode, string? TaxRegistrationNumber,
    string? CommercialRegistrationNumber, string? CommercialRegistry, string? Phone, string? Mobile, string? Email, string? Notes,
    int PaymentTermDays, decimal? CreditLimit, bool IsOnCreditHold, string? CreditHoldReason, DateTime? CreditChangedAtUtc,
    string? CreditChangedBy, bool IsActive, IReadOnlyList<CustomerContactResponse> Contacts,
    IReadOnlyList<CustomerAddressResponse> Addresses, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, string RowVersion);
