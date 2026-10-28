namespace WarehouseERP.Organization.Contracts;

/// <summary>A country as other modules see it.</summary>
/// <param name="Code">ISO 3166 alpha-2 code, upper case.</param>
/// <param name="NameAr">Arabic name.</param>
/// <param name="NameEn">English name, when there is one.</param>
/// <param name="IsActive">False once it is no longer offered for new addresses.</param>
public sealed record CountryInfo(string Code, string NameAr, string? NameEn, bool IsActive);
