using Microsoft.EntityFrameworkCore;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Infrastructure.Persistence;

internal sealed class CustomerRepository(DbContext db) : ICustomerRepository
{
    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        db.Set<Customer>().AnyAsync(x => x.Code == code, ct);

    public Task<bool> TaxNumberTakenAsync(string countryCode, string taxNumber, Guid? exceptCustomerId, CancellationToken ct) =>
        db.Set<Customer>().AnyAsync(x =>
            x.CountryCode == countryCode && x.TaxRegistrationNumber == taxNumber && (exceptCustomerId == null || x.Id != exceptCustomerId), ct);

    public Task<bool> CommercialRegistrationTakenAsync(
        string countryCode, string registryKey, string registrationNumber, Guid? exceptCustomerId, CancellationToken ct) =>
        db.Set<Customer>().AnyAsync(x =>
            x.CountryCode == countryCode && x.CommercialRegistryKey == registryKey && x.CommercialRegistrationNumber == registrationNumber
            && (exceptCustomerId == null || x.Id != exceptCustomerId), ct);

    public Task<CustomerState?> FindStateAsync(Guid customerId, CancellationToken ct) =>
        db.Set<Customer>().AsNoTracking()
            .Where(x => x.Id == customerId)
            .Select(x => new CustomerState(x.Id, x.Code, x.NameAr, x.IsActive, x.PaymentTermDays, x.CreditLimit, x.IsOnCreditHold))
            .SingleOrDefaultAsync(ct);

    public Task<Result> AddAsync(Customer customer, CancellationToken ct)
    {
        db.Set<Customer>().Add(customer);
        return SalesSaves.SaveAsync(db, ct);
    }

    public async Task<Customer?> GetForUpdateAsync(Guid customerId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var customer = await db.Set<Customer>()
            .Include(x => x.Contacts)
            .Include(x => x.Addresses)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == customerId, ct);
        if (customer is not null && expectedRowVersion is not null)
            // The UPDATE's WHERE uses the original value, so this makes it compare with the caller's version.
            db.Entry(customer).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return customer;
    }

    public Task<Result> SaveChangesAsync(CancellationToken ct) => SalesSaves.SaveAsync(db, ct);
}
