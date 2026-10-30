using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.ReadModels;

// What other modules may learn about a supplier: a projection, never the entity.
internal sealed class SupplierDirectory(DbContext db) : ISupplierDirectory
{
    private static readonly Expression<Func<Supplier, SupplierInfo>> ToInfo =
        x => new SupplierInfo(x.Id, x.Code, x.NameAr, x.NameEn, x.TaxRegistrationNumber, x.PaymentTermDays, x.IsActive);

    public async Task<IReadOnlyList<SupplierInfo>> FindManyAsync(IReadOnlyCollection<Guid> supplierIds, CancellationToken ct)
    {
        if (supplierIds.Count == 0)
            return [];
        var ids = supplierIds.ToArray();
        return await db.Set<Supplier>().AsNoTracking().Where(x => ids.Contains(x.Id)).Select(ToInfo).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SupplierInfo>> SearchActiveAsync(string text, int take, CancellationToken ct)
    {
        var search = text.Trim();
        if (search.Length == 0 || take <= 0)
            return [];
        return await db.Set<Supplier>().AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => x.Code.Contains(search) || x.NameAr.Contains(search) || (x.NameEn != null && x.NameEn.Contains(search))
                        || (x.TaxRegistrationNumber != null && x.TaxRegistrationNumber.Contains(search)))
            .OrderBy(x => x.Code)
            .Take(take)
            .Select(ToInfo)
            .ToListAsync(ct);
    }
}
