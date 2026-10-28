namespace WarehouseERP.Organization.Application.Features.Geography.ListCountries;

/// <param name="IsActive">Only active or only inactive countries; null for all.</param>
public sealed record ListCountriesQuery(bool? IsActive);
