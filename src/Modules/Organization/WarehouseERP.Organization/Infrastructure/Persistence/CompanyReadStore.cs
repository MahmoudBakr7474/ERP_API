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
            })
            .SingleOrDefaultAsync(ct);
        return row is null
            ? null
            : new CompanyResponse(row.Id, row.Code, row.NameAr, row.NameEn, row.BaseCurrencyCode, row.TimeZoneId,
                row.TaxRegistrationNumber, Convert.ToBase64String(row.RowVersion));
    }
}
