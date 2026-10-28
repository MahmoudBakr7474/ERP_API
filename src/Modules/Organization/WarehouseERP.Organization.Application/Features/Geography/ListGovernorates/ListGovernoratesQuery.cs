namespace WarehouseERP.Organization.Application.Features.Geography.ListGovernorates;

/// <param name="CountryCode">ISO 3166 alpha-2 code of the country.</param>
/// <param name="IsActive">Only active or only inactive governorates; null for all.</param>
public sealed record ListGovernoratesQuery(string CountryCode, bool? IsActive);
