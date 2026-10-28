using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Contracts.Geography;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence;

internal sealed class GeographyReadStore(DbContext db) : IGeographyReadStore
{
    public async Task<IReadOnlyList<CountryResponse>> ListCountriesAsync(bool? isActive, CancellationToken ct)
    {
        var rows = await db.Set<Country>().AsNoTracking()
            .Where(x => isActive == null || x.IsActive == isActive)
            .OrderBy(x => x.NameAr)
            .Select(x => new { x.Id, x.Code, x.NameAr, x.NameEn, x.IsActive, x.RowVersion })
            .ToListAsync(ct);
        return rows.Select(x => new CountryResponse(x.Id, x.Code, x.NameAr, x.NameEn, x.IsActive, Convert.ToBase64String(x.RowVersion)))
            .ToList();
    }

    public async Task<IReadOnlyList<GovernorateResponse>?> ListGovernoratesAsync(string countryCode, bool? isActive, CancellationToken ct)
    {
        var countryId = await db.Set<Country>().AsNoTracking()
            .Where(x => x.Code == countryCode).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (countryId is null)
            return null;

        var rows = await db.Set<Governorate>().AsNoTracking()
            .Where(x => x.CountryId == countryId && (isActive == null || x.IsActive == isActive))
            .OrderBy(x => x.NameAr)
            .Select(x => new { x.Id, x.NameAr, x.NameEn, x.IsActive, x.RowVersion })
            .ToListAsync(ct);
        return rows.Select(x => new GovernorateResponse(
                x.Id, countryCode, x.NameAr, x.NameEn, x.IsActive, Convert.ToBase64String(x.RowVersion)))
            .ToList();
    }

    public async Task<IReadOnlyList<CityResponse>?> ListCitiesAsync(Guid governorateId, bool? isActive, CancellationToken ct)
    {
        if (!await db.Set<Governorate>().AnyAsync(x => x.Id == governorateId, ct))
            return null;

        var rows = await db.Set<City>().AsNoTracking()
            .Where(x => x.GovernorateId == governorateId && (isActive == null || x.IsActive == isActive))
            .OrderBy(x => x.NameAr)
            .Select(x => new { x.Id, x.NameAr, x.NameEn, x.IsActive, x.RowVersion })
            .ToListAsync(ct);
        return rows.Select(x => new CityResponse(
                x.Id, governorateId, x.NameAr, x.NameEn, x.IsActive, Convert.ToBase64String(x.RowVersion)))
            .ToList();
    }
}
