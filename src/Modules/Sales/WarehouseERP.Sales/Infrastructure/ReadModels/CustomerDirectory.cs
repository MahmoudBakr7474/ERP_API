using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.ReadModels;

// What other modules may learn about a customer: a projection, never the entity.
internal sealed class CustomerDirectory(DbContext db) : ICustomerDirectory
{
    private static readonly Expression<Func<Customer, SalesCustomerInfo>> ToInfo =
        x => new SalesCustomerInfo(x.Id, x.Code, x.NameAr, x.NameEn, x.CustomerType, x.TaxRegistrationNumber, x.Mobile, x.IsActive);

    public Task<SalesCustomerInfo?> FindAsync(Guid customerId, CancellationToken ct) =>
        db.Set<Customer>().AsNoTracking().Where(x => x.Id == customerId).Select(ToInfo).SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SalesCustomerInfo>> FindManyAsync(IReadOnlyCollection<Guid> customerIds, CancellationToken ct)
    {
        if (customerIds.Count == 0)
            return [];
        var ids = customerIds.ToArray();
        return await db.Set<Customer>().AsNoTracking().Where(x => ids.Contains(x.Id)).Select(ToInfo).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SalesCustomerInfo>> SearchActiveAsync(string text, int take, CancellationToken ct)
    {
        var search = text.Trim();
        if (search.Length == 0 || take <= 0)
            return [];
        var phone = CustomerNames.PhoneKey(search);
        return await db.Set<Customer>().AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => x.Code.Contains(search) || x.NameAr.Contains(search) || (x.NameEn != null && x.NameEn.Contains(search))
                        || (x.TaxRegistrationNumber != null && x.TaxRegistrationNumber.Contains(search))
                        || (x.Phone != null && x.Phone.Contains(search))
                        || (phone != null && x.MobileKey != null && x.MobileKey.Contains(phone)))
            .OrderBy(x => x.Code)
            .Take(take)
            .Select(ToInfo)
            .ToListAsync(ct);
    }
}
