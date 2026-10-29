namespace WarehouseERP.Sales.Domain;

/// <summary>Everything about a customer that may change after it is created, credit terms apart.</summary>
/// <param name="CountryCode">ISO 3166 alpha-2 country of its registrations.</param>
/// <param name="TaxRegistrationNumber">As typed; spaces, dashes, dots and slashes are dropped, Arabic digits read as digits.</param>
/// <param name="CommercialRegistrationNumber">Businesses only; normalized like the tax number.</param>
/// <param name="CommercialRegistry">The office that issued the commercial registration; required with its number.</param>
public sealed record CustomerDetails(
    string NameAr, string? NameEn, string CountryCode, string? TaxRegistrationNumber, string? CommercialRegistrationNumber,
    string? CommercialRegistry, string? Phone, string? Mobile, string? Email, string? Notes,
    IReadOnlyList<CustomerContactInput> Contacts, IReadOnlyList<CustomerAddressInput> Addresses);
