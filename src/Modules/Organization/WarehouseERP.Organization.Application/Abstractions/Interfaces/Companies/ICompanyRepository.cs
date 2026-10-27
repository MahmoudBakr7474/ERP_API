using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Companies;

/// <summary>Write side of the company. V1 has exactly one, set up once.</summary>
public interface ICompanyRepository
{
    /// <summary>
    /// Save point of the setup: adds the company unless one already exists. The check holds a lock until the
    /// save commits, so two setups at the same time can't both succeed.
    /// </summary>
    Task<Result> AddIfNoneAsync(Company company, CancellationToken ct);

    /// <summary>
    /// The company, tracked for changes, or null until it is set up. With a row version, the save fails with a
    /// conflict unless the company still has it.
    /// </summary>
    Task<Company?> GetForUpdateAsync(byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>Save point for changes to the company loaded with <see cref="GetForUpdateAsync"/>.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
