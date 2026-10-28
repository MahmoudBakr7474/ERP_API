using WarehouseERP.Purchasing.Application.Contracts.Suppliers;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers.UpdateSupplier;

/// <summary>Everything but the code; contacts and addresses are replaced by the lists given.</summary>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdateSupplierCommand(
    Guid SupplierId, string NameAr, string? NameEn, string CountryCode, string? TaxRegistrationNumber,
    string? CommercialRegistrationNumber, string? CommercialRegistry, int PaymentTermDays, string? Phone, string? Email,
    string? Notes, IReadOnlyList<SupplierContactRequest>? Contacts, IReadOnlyList<SupplierAddressRequest>? Addresses,
    string RowVersion);
