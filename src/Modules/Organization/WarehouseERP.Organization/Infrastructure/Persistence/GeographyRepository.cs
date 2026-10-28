using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Infrastructure.Persistence;

internal sealed class GeographyRepository(DbContext db) : IGeographyRepository
{
    // Unique keys whose violation is an expected business conflict. Names come from Migration08.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["AK_Countries_Code"] = GeographyErrors.CountryCodeTaken,
        ["UX_Countries_NameAr"] = GeographyErrors.CountryNameTaken,
        ["UX_Governorates_CountryId_NameAr"] = GeographyErrors.GovernorateNameTaken,
        ["UX_Cities_GovernorateId_NameAr"] = GeographyErrors.CityNameTaken,
    };

    public Task<Country?> GetCountryAsync(Guid countryId, byte[]? expectedRowVersion, CancellationToken ct) =>
        GetAsync<Country>(countryId, expectedRowVersion, ct);

    public Task<Country?> GetCountryByCodeAsync(string code, CancellationToken ct) =>
        db.Set<Country>().SingleOrDefaultAsync(x => x.Code == code, ct);

    public Task<Governorate?> GetGovernorateAsync(Guid governorateId, byte[]? expectedRowVersion, CancellationToken ct) =>
        GetAsync<Governorate>(governorateId, expectedRowVersion, ct);

    public Task<City?> GetCityAsync(Guid cityId, byte[]? expectedRowVersion, CancellationToken ct) =>
        GetAsync<City>(cityId, expectedRowVersion, ct);

    public void Add(Country country) => db.Set<Country>().Add(country);

    public void Add(Governorate governorate) => db.Set<Governorate>().Add(governorate);

    public void Add(City city) => db.Set<City>().Add(city);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);

    private async Task<T?> GetAsync<T>(Guid id, byte[]? expectedRowVersion, CancellationToken ct) where T : MasterEntity
    {
        var record = await db.Set<T>().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (record is not null && expectedRowVersion is not null)
            // The UPDATE's WHERE uses the original value, so this makes it compare with the caller's version.
            db.Entry(record).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return record;
    }
}
