using Microsoft.EntityFrameworkCore;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence;

internal sealed class SupplierRepository(DbContext db) : ISupplierRepository
{
    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        db.Set<Supplier>().AnyAsync(x => x.Code == code, ct);

    public Task<bool> TaxNumberTakenAsync(string countryCode, string taxNumber, Guid? exceptSupplierId, CancellationToken ct) =>
        db.Set<Supplier>().AnyAsync(x =>
            x.CountryCode == countryCode && x.TaxRegistrationNumber == taxNumber && (exceptSupplierId == null || x.Id != exceptSupplierId), ct);

    public Task<bool> CommercialRegistrationTakenAsync(
        string countryCode, string registryKey, string registrationNumber, Guid? exceptSupplierId, CancellationToken ct) =>
        db.Set<Supplier>().AnyAsync(x =>
            x.CountryCode == countryCode && x.CommercialRegistryKey == registryKey && x.CommercialRegistrationNumber == registrationNumber
            && (exceptSupplierId == null || x.Id != exceptSupplierId), ct);

    public Task<SupplierState?> FindStateAsync(Guid supplierId, CancellationToken ct) =>
        db.Set<Supplier>().AsNoTracking()
            .Where(x => x.Id == supplierId)
            .Select(x => new SupplierState(x.Id, x.Code, x.NameAr, x.IsActive, x.PaymentTermDays))
            .SingleOrDefaultAsync(ct);

    public Task<Result> AddAsync(Supplier supplier, CancellationToken ct)
    {
        db.Set<Supplier>().Add(supplier);
        return PurchasingSaves.SaveAsync(db, ct);
    }

    public async Task<Supplier?> GetForUpdateAsync(Guid supplierId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var supplier = await db.Set<Supplier>()
            .Include(x => x.Contacts)
            .Include(x => x.Addresses)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == supplierId, ct);
        if (supplier is not null && expectedRowVersion is not null)
            // The UPDATE's WHERE uses the original value, so this makes it compare with the caller's version.
            db.Entry(supplier).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return supplier;
    }

    public Task<Result> SaveChangesAsync(CancellationToken ct) => PurchasingSaves.SaveAsync(db, ct);
}
