using WarehouseERP.Sales.Application.Contracts.Customers;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Features.Customers.CreateCustomer;

/// <summary>A new customer, on cash terms with no credit limit until someone with sales.credit.manage sets them.</summary>
/// <param name="Code">Unique and fixed once created; letters, digits, '-' and '_' (stored upper case).</param>
/// <param name="CustomerType">Individual or business; fixed once created.</param>
/// <param name="CountryCode">ISO 3166 alpha-2 country of the registrations below.</param>
/// <param name="TaxRegistrationNumber">Unique per country once normalized; an Egyptian one has 9 digits.</param>
/// <param name="CommercialRegistrationNumber">Businesses only; unique per country and registry office once normalized.</param>
/// <param name="CommercialRegistry">The registry office that issued it; required with the number.</param>
/// <param name="Mobile">Customers sharing a mobile number are reported as possible duplicates.</param>
public sealed record CreateCustomerCommand(
    string Code, CustomerType CustomerType, string NameAr, string? NameEn, string CountryCode, string? TaxRegistrationNumber,
    string? CommercialRegistrationNumber, string? CommercialRegistry, string? Phone, string? Mobile, string? Email, string? Notes,
    IReadOnlyList<CustomerContactRequest>? Contacts = null, IReadOnlyList<CustomerAddressRequest>? Addresses = null);
