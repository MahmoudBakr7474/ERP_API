namespace WarehouseERP.Organization.Contracts;

/// <summary>Read-only view of the address lookups (countries, governorates, cities) for other modules.</summary>
public interface IGeographyDirectory
{
    /// <summary>The country with this ISO 3166 alpha-2 code, or null when there is none.</summary>
    Task<CountryInfo?> FindCountryAsync(string countryCode, CancellationToken ct);

    /// <summary>The countries among these codes that exist; unknown codes are left out.</summary>
    Task<IReadOnlyList<CountryInfo>> FindCountriesAsync(IReadOnlyCollection<string> countryCodes, CancellationToken ct);

    /// <summary>The cities among these ids that exist, with their governorate and country; unknown ids are left out.</summary>
    Task<IReadOnlyList<CityInfo>> FindCitiesAsync(IReadOnlyCollection<Guid> cityIds, CancellationToken ct);
}
