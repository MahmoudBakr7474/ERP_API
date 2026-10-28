using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;
using WarehouseERP.Purchasing.Application.Contracts.Suppliers;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.ReadModels;

// Address names (city, governorate, country) come from Organization's lookups as they are now.
internal sealed class SupplierReadStore(DbContext db, IGeographyDirectory geography) : ISupplierReadStore
{
    private const int MaxSimilar = 20;

    public async Task<SupplierDetailsResponse?> GetAsync(Guid supplierId, CancellationToken ct)
    {
        var supplier = await db.Set<Supplier>().AsNoTracking()
            .Include(x => x.Contacts)
            .Include(x => x.Addresses)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == supplierId, ct);
        if (supplier is null)
            return null;
        var cities = (await geography.FindCitiesAsync(supplier.Addresses.Select(a => a.CityId).ToList(), ct)).ToDictionary(c => c.Id);

        return new SupplierDetailsResponse(
            supplier.Id, supplier.Code, supplier.NameAr, supplier.NameEn, supplier.CountryCode, supplier.TaxRegistrationNumber,
            supplier.CommercialRegistrationNumber, supplier.CommercialRegistry, supplier.PaymentTermDays, supplier.Phone, supplier.Email,
            supplier.Notes, supplier.IsActive,
            supplier.Contacts.OrderByDescending(c => c.IsPrimary).ThenBy(c => c.Name)
                .Select(c => new SupplierContactResponse(c.Id, c.Name, c.JobTitle, c.Phone, c.Email, c.IsPrimary)).ToList(),
            supplier.Addresses.OrderByDescending(a => a.IsPrimary).ThenBy(a => a.AddressType)
                .Select(a => Address(a, cities[a.CityId]))
                .ToList(),
            supplier.CreatedAtUtc, supplier.UpdatedAtUtc, Convert.ToBase64String(supplier.RowVersion));
    }

    // An address's city always exists: the foreign key restricts deleting it, and the lookups are never hard-deleted.
    private static SupplierAddressResponse Address(SupplierAddress a, CityInfo city) => new(
        a.Id, a.AddressType, a.Line1, a.Line2, city.CountryCode, city.CountryNameAr, city.CountryNameEn,
        city.GovernorateId, city.GovernorateNameAr, city.GovernorateNameEn, city.Id, city.NameAr, city.NameEn, a.PostalCode, a.IsPrimary);

    public async Task<PagedResult<SupplierSummaryResponse>> ListAsync(SupplierFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<Supplier>().AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(x => x.IsActive == isActive);
        if (filter.CountryCode is { } country)
            query = query.Where(x => x.CountryCode == country);
        if (filter.Search is { } search)
            query = query.Where(x => x.Code.Contains(search) || x.NameAr.Contains(search) || (x.NameEn != null && x.NameEn.Contains(search))
                                     || (x.TaxRegistrationNumber != null && x.TaxRegistrationNumber.Contains(search)));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Code)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new SupplierSummaryResponse(
                x.Id, x.Code, x.NameAr, x.NameEn, x.CountryCode, x.TaxRegistrationNumber, x.Phone, x.PaymentTermDays, x.IsActive))
            .ToListAsync(ct);
        return new PagedResult<SupplierSummaryResponse>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<SimilarSupplierResponse>> FindSimilarAsync(
        IReadOnlyCollection<string> nameKeys, Guid? exceptSupplierId, CancellationToken ct)
    {
        var keys = nameKeys.ToArray();
        return await db.Set<Supplier>().AsNoTracking()
            .Where(x => (keys.Contains(x.NameArKey) || (x.NameEnKey != null && keys.Contains(x.NameEnKey)))
                        && (exceptSupplierId == null || x.Id != exceptSupplierId))
            .OrderBy(x => x.Code)
            .Take(MaxSimilar)
            .Select(x => new SimilarSupplierResponse(x.Id, x.Code, x.NameAr, x.NameEn, x.TaxRegistrationNumber, x.IsActive))
            .ToListAsync(ct);
    }
}
