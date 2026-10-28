using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;

/// <summary>
/// Write side of countries, governorates and cities over the shared ErpDbContext. The Get methods return the record
/// tracked for changes, or null; with a row version, the save fails with a conflict unless the record still has it.
/// </summary>
public interface IGeographyRepository
{
    Task<Country?> GetCountryAsync(Guid countryId, byte[]? expectedRowVersion, CancellationToken ct);

    Task<Country?> GetCountryByCodeAsync(string code, CancellationToken ct);

    Task<Governorate?> GetGovernorateAsync(Guid governorateId, byte[]? expectedRowVersion, CancellationToken ct);

    Task<City?> GetCityAsync(Guid cityId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>Queues a new country, governorate or city for the next save.</summary>
    void Add(Country country);

    /// <inheritdoc cref="Add(Country)"/>
    void Add(Governorate governorate);

    /// <inheritdoc cref="Add(Country)"/>
    void Add(City city);

    /// <summary>The single save point. A known unique-key violation comes back as its error, not an exception.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
