using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;

/// <summary>Write side of customers with their contacts and addresses; implemented over the shared ErpDbContext.</summary>
public interface ICustomerRepository
{
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    /// <summary>True when another customer of the country has this normalized tax registration number.</summary>
    Task<bool> TaxNumberTakenAsync(string countryCode, string taxNumber, Guid? exceptCustomerId, CancellationToken ct);

    /// <summary>True when another customer has this normalized commercial registration at the same country and registry.</summary>
    Task<bool> CommercialRegistrationTakenAsync(
        string countryCode, string registryKey, string registrationNumber, Guid? exceptCustomerId, CancellationToken ct);

    /// <summary>What sales documents need to know about a customer, or null.</summary>
    Task<CustomerState?> FindStateAsync(Guid customerId, CancellationToken ct);

    /// <summary>Save point for a new customer. A known unique-key violation comes back as its error.</summary>
    Task<Result> AddAsync(Customer customer, CancellationToken ct);

    /// <summary>
    /// The customer with its contacts and addresses, tracked, or null. With a row version, the save fails with a conflict
    /// unless the customer still has it.
    /// </summary>
    Task<Customer?> GetForUpdateAsync(Guid customerId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>
    /// Save point for changes to a customer loaded with <see cref="GetForUpdateAsync"/>; replacing only its contacts or
    /// addresses still moves its row version.
    /// </summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
