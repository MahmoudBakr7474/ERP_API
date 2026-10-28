namespace WarehouseERP.Organization.Application.Features.Geography.CreateCountry;

/// <param name="Code">ISO 3166 alpha-2; fixed once created.</param>
public sealed record CreateCountryCommand(string Code, string NameAr, string? NameEn);
