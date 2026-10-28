using WarehouseERP.Purchasing.Application.Contracts.Suppliers;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers.CreateSupplier;

/// <param name="Code">Unique and fixed once created; letters, digits, '-' and '_' (stored upper case).</param>
/// <param name="CountryCode">ISO 3166 alpha-2 country of the registrations below.</param>
/// <param name="TaxRegistrationNumber">Unique per country once normalized; an Egyptian one has 9 digits.</param>
/// <param name="CommercialRegistrationNumber">Unique per country and registry office once normalized.</param>
/// <param name="CommercialRegistry">The registry office that issued it; required with the number.</param>
/// <param name="PaymentTermDays">Days from invoice date to due date, 0 to 365.</param>
public sealed record CreateSupplierCommand(
    string Code, string NameAr, string? NameEn, string CountryCode, string? TaxRegistrationNumber,
    string? CommercialRegistrationNumber, string? CommercialRegistry, int PaymentTermDays, string? Phone, string? Email,
    string? Notes, IReadOnlyList<SupplierContactRequest>? Contacts = null, IReadOnlyList<SupplierAddressRequest>? Addresses = null);
