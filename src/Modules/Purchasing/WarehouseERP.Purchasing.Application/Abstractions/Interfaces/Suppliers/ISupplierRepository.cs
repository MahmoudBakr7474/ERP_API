using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;

/// <summary>Write side of suppliers with their contacts and addresses; implemented over the shared ErpDbContext.</summary>
public interface ISupplierRepository
{
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    /// <summary>True when another supplier of the country has this normalized tax registration number.</summary>
    Task<bool> TaxNumberTakenAsync(string countryCode, string taxNumber, Guid? exceptSupplierId, CancellationToken ct);

    /// <summary>True when another supplier has this normalized commercial registration at the same country and registry.</summary>
    Task<bool> CommercialRegistrationTakenAsync(
        string countryCode, string registryKey, string registrationNumber, Guid? exceptSupplierId, CancellationToken ct);

    /// <summary>What other purchasing documents need to know about a supplier, or null.</summary>
    Task<SupplierState?> FindStateAsync(Guid supplierId, CancellationToken ct);

    /// <summary>Save point for a new supplier. A known unique-key violation comes back as its error.</summary>
    Task<Result> AddAsync(Supplier supplier, CancellationToken ct);

    /// <summary>
    /// The supplier with its contacts and addresses, tracked, or null. With a row version, the save fails with a conflict
    /// unless the supplier still has it.
    /// </summary>
    Task<Supplier?> GetForUpdateAsync(Guid supplierId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>
    /// Save point for changes to a supplier loaded with <see cref="GetForUpdateAsync"/>; replacing only its contacts or
    /// addresses still moves its row version.
    /// </summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
