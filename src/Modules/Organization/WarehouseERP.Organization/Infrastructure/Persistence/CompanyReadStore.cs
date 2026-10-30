using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Companies;
using WarehouseERP.Organization.Application.Contracts.Companies;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence;

internal sealed class CompanyReadStore(DbContext db) : ICompanyReadStore
{
    // SingleOrDefault: a second company would be a data error, never one picked silently.
    public async Task<CompanyResponse?> GetAsync(CancellationToken ct)
    {
        var row = await db.Set<Company>().AsNoTracking()
            .Select(x => new
            {
                x.Id, x.Code, x.NameAr, x.NameEn, x.BaseCurrencyCode, x.TimeZoneId, x.TaxRegistrationNumber, x.RowVersion,
                x.CommercialRegistrationNumber, x.CommercialRegistry, x.Phone, x.Email, x.AddressLine, x.CityId,
            })
            .SingleOrDefaultAsync(ct);
        if (row is null)
            return null;

        var city = row.CityId is not { } cityId
            ? null
            : await (from c in db.Set<City>().AsNoTracking()
                     join g in db.Set<Governorate>().AsNoTracking() on c.GovernorateId equals g.Id
                     join k in db.Set<Country>().AsNoTracking() on g.CountryId equals k.Id
                     where c.Id == cityId
                     select new CompanyCityResponse(c.Id, c.NameAr, c.NameEn, g.Id, g.NameAr, g.NameEn, k.Code, k.NameAr, k.NameEn))
                .SingleOrDefaultAsync(ct);
        return new CompanyResponse(row.Id, row.Code, row.NameAr, row.NameEn, row.BaseCurrencyCode, row.TimeZoneId,
            row.TaxRegistrationNumber, Convert.ToBase64String(row.RowVersion), row.CommercialRegistrationNumber,
            row.CommercialRegistry, row.Phone, row.Email, row.AddressLine, city);
    }
}
