using WarehouseERP.Sales.Application.Contracts.Customers;

namespace WarehouseERP.Sales.Application.Features.Customers.UpdateCustomer;

/// <summary>Everything but the code, the type and the credit terms; contacts and addresses are replaced by the lists given.</summary>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdateCustomerCommand(
    Guid CustomerId, string NameAr, string? NameEn, string CountryCode, string? TaxRegistrationNumber,
    string? CommercialRegistrationNumber, string? CommercialRegistry, string? Phone, string? Mobile, string? Email, string? Notes,
    IReadOnlyList<CustomerContactRequest>? Contacts, IReadOnlyList<CustomerAddressRequest>? Addresses, string RowVersion);
