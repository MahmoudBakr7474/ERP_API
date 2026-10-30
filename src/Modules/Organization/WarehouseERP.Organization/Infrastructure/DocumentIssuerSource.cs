using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure;

// The issuing company printed on every generated document: its own names, tax number and time zone. V1 has one
// company; until it is set up documents print without company details rather than invented ones.
internal sealed class DocumentIssuerSource(DbContext db) : IDocumentIssuerSource
{
    public Task<DocumentIssuer?> GetAsync(CancellationToken ct) =>
        db.Set<Company>().AsNoTracking()
            .Select(x => new DocumentIssuer(x.NameAr, x.NameEn, x.TaxRegistrationNumber, x.TimeZoneId))
            .FirstOrDefaultAsync(ct);
}
