using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;

/// <summary>The module's save point: one SaveChanges for everything the use case changed, one transaction.</summary>
public interface IIdentityPersistence
{
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
