using WarehouseERP.Organization.Application.Contracts.Geography;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;

/// <summary>
/// Read side of the address lookups: projections, no tracking. The lists are small (a country's governorates, a
/// governorate's cities), so they come whole, ordered by Arabic name; isActive null means both.
/// </summary>
public interface IGeographyReadStore
{
    Task<IReadOnlyList<CountryResponse>> ListCountriesAsync(bool? isActive, CancellationToken ct);

    /// <summary>The governorates of the country with this code; null when there is no such country.</summary>
    Task<IReadOnlyList<GovernorateResponse>?> ListGovernoratesAsync(string countryCode, bool? isActive, CancellationToken ct);

    /// <summary>The cities of this governorate; null when there is no such governorate.</summary>
    Task<IReadOnlyList<CityResponse>?> ListCitiesAsync(Guid governorateId, bool? isActive, CancellationToken ct);
}
