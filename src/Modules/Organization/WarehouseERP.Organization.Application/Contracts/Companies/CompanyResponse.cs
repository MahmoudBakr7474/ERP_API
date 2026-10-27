namespace WarehouseERP.Organization.Application.Contracts.Companies;

/// <param name="RowVersion">Base64 row version; send it back with the next update.</param>
public sealed record CompanyResponse(
    Guid Id, string Code, string NameAr, string? NameEn, string BaseCurrencyCode, string TimeZoneId,
    string? TaxRegistrationNumber, string RowVersion);
