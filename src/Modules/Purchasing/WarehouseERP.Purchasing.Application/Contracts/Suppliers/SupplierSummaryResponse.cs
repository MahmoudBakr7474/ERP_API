namespace WarehouseERP.Purchasing.Application.Contracts.Suppliers;

public sealed record SupplierSummaryResponse(
    Guid Id, string Code, string NameAr, string? NameEn, string CountryCode, string? TaxRegistrationNumber, string? Phone,
    int PaymentTermDays, bool IsActive);
