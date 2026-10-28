using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure;

// What other modules may learn about countries, governorates and cities: projections, never the entities.
internal sealed class GeographyDirectory(DbContext db) : IGeographyDirectory
{
    public Task<CountryInfo?> FindCountryAsync(string countryCode, CancellationToken ct)
    {
        var code = countryCode.Trim().ToUpperInvariant();
        return db.Set<Country>().AsNoTracking()
            .Where(x => x.Code == code)
            .Select(x => new CountryInfo(x.Code, x.NameAr, x.NameEn, x.IsActive))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<CountryInfo>> FindCountriesAsync(IReadOnlyCollection<string> countryCodes, CancellationToken ct)
    {
        var codes = countryCodes.Select(c => c.Trim().ToUpperInvariant()).Distinct().ToArray();
        return codes.Length == 0
            ? []
            : await db.Set<Country>().AsNoTracking()
                .Where(x => codes.Contains(x.Code))
                .Select(x => new CountryInfo(x.Code, x.NameAr, x.NameEn, x.IsActive))
                .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CityInfo>> FindCitiesAsync(IReadOnlyCollection<Guid> cityIds, CancellationToken ct)
    {
        var ids = cityIds.Distinct().ToArray();
        return ids.Length == 0
            ? []
            : await (from city in db.Set<City>().AsNoTracking()
                     join governorate in db.Set<Governorate>().AsNoTracking() on city.GovernorateId equals governorate.Id
                     join country in db.Set<Country>().AsNoTracking() on governorate.CountryId equals country.Id
                     where ids.Contains(city.Id)
                     select new CityInfo(
                         city.Id, city.NameAr, city.NameEn, city.IsActive,
                         governorate.Id, governorate.NameAr, governorate.NameEn, governorate.IsActive,
                         country.Code, country.NameAr, country.NameEn, country.IsActive))
                .ToListAsync(ct);
    }
}
