namespace WarehouseERP.Purchasing.Domain;

/// <summary>Everything about a supplier that may change after it is created.</summary>
/// <param name="CountryCode">ISO 3166 alpha-2 country of its registrations.</param>
/// <param name="TaxRegistrationNumber">As typed; spaces, dashes, dots and slashes are dropped, Arabic digits read as digits.</param>
/// <param name="CommercialRegistrationNumber">As typed; normalized like the tax number.</param>
/// <param name="CommercialRegistry">The office that issued the commercial registration; required with its number.</param>
/// <param name="PaymentTermDays">Days from invoice date to due date, 0 to 365.</param>
public sealed record SupplierDetails(
    string NameAr, string? NameEn, string CountryCode, string? TaxRegistrationNumber, string? CommercialRegistrationNumber,
    string? CommercialRegistry, int PaymentTermDays, string? Phone, string? Email, string? Notes,
    IReadOnlyList<SupplierContactInput> Contacts, IReadOnlyList<SupplierAddressInput> Addresses);
