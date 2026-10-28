using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure;

// What other modules may learn about the company: a projection, never the entity. V1 has one company.
internal sealed class CompanyDirectory(DbContext db) : ICompanyDirectory
{
    public Task<CompanyInfo?> GetAsync(CancellationToken ct) =>
        db.Set<Company>().AsNoTracking()
            .Select(x => new CompanyInfo(x.Id, x.Code, x.NameAr, x.BaseCurrencyCode, x.TimeZoneId))
            .FirstOrDefaultAsync(ct);
}
