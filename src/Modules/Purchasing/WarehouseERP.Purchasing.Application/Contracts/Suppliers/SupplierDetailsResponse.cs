namespace WarehouseERP.Purchasing.Application.Contracts.Suppliers;

/// <param name="TaxRegistrationNumber">Normalized: letters and digits only.</param>
/// <param name="CommercialRegistry">The registry office that issued the commercial registration.</param>
/// <param name="RowVersion">Send it back with the next update.</param>
public sealed record SupplierDetailsResponse(
    Guid Id, string Code, string NameAr, string? NameEn, string CountryCode, string? TaxRegistrationNumber,
    string? CommercialRegistrationNumber, string? CommercialRegistry, int PaymentTermDays, string? Phone, string? Email,
    string? Notes, bool IsActive, IReadOnlyList<SupplierContactResponse> Contacts, IReadOnlyList<SupplierAddressResponse> Addresses,
    DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, string RowVersion);
