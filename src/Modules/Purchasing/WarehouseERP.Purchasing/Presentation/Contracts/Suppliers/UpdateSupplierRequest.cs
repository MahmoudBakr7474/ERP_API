using WarehouseERP.Purchasing.Application.Contracts.Suppliers;

namespace WarehouseERP.Purchasing.Presentation.Contracts.Suppliers;

/// <summary>Everything but the code; contacts and addresses are replaced by the lists given.</summary>
public sealed record UpdateSupplierRequest(
    string NameAr, string? NameEn, string CountryCode, string? TaxRegistrationNumber, string? CommercialRegistrationNumber,
    string? CommercialRegistry, int PaymentTermDays, string? Phone, string? Email, string? Notes,
    IReadOnlyList<SupplierContactRequest>? Contacts, IReadOnlyList<SupplierAddressRequest>? Addresses, string RowVersion);
