namespace WarehouseERP.Organization.Application.Contracts.Geography;

/// <param name="Code">ISO 3166 alpha-2 code; what addresses and suppliers store.</param>
/// <param name="RowVersion">Base64; send it back to rename.</param>
public sealed record CountryResponse(Guid Id, string Code, string NameAr, string? NameEn, bool IsActive, string RowVersion);
