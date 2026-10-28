namespace WarehouseERP.Organization.Application.Features.Geography.CreateGovernorate;

/// <param name="CountryCode">ISO 3166 alpha-2 code of an active country; fixed once created.</param>
public sealed record CreateGovernorateCommand(string CountryCode, string NameAr, string? NameEn);
