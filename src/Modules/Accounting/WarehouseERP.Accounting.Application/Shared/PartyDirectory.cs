using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Accounting.Application.Shared;

// Accounting's parties stay owned by their modules: customers are Sales', suppliers Purchasing's. Accounting keeps only
// their ids on lines and open items and asks the owners for codes and names.
public sealed class PartyDirectory(ICustomerDirectory customers, ISupplierDirectory suppliers)
{
    /// <summary>The parties among these keys that exist, by type and id.</summary>
    public async Task<IReadOnlyDictionary<(PartyType Type, Guid Id), PartyInfo>> FindAsync(
        IEnumerable<(PartyType Type, Guid Id)> parties, CancellationToken ct)
    {
        var keys = parties.Distinct().ToList();
        var found = new Dictionary<(PartyType, Guid), PartyInfo>();
        var customerIds = keys.Where(k => k.Type == PartyType.Customer).Select(k => k.Id).ToList();
        if (customerIds.Count > 0)
            foreach (var c in await customers.FindManyAsync(customerIds, ct))
                found[(PartyType.Customer, c.Id)] = new PartyInfo(PartyType.Customer, c.Id, c.Code, c.NameAr, c.NameEn, c.IsActive, 0);
        var supplierIds = keys.Where(k => k.Type == PartyType.Supplier).Select(k => k.Id).ToList();
        if (supplierIds.Count > 0)
            foreach (var s in await suppliers.FindManyAsync(supplierIds, ct))
                found[(PartyType.Supplier, s.Id)] = new PartyInfo(PartyType.Supplier, s.Id, s.Code, s.NameAr, s.NameEn, s.IsActive, s.PaymentTermDays);
        return found;
    }

    public async Task<PartyInfo?> FindAsync(PartyType type, Guid id, CancellationToken ct) =>
        (await FindAsync([(type, id)], ct)).GetValueOrDefault((type, id));
}
