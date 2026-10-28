using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;

/// <summary>Write side of supplier returns with their lines and history.</summary>
public interface ISupplierReturnRepository
{
    Task<Guid?> FindWarehouseAsync(Guid returnId, CancellationToken ct);

    /// <summary>
    /// The return with its lines, tracked and locked against other writers until the transaction ends, or null. With a
    /// row version, the save fails with a conflict unless the return still has it.
    /// </summary>
    Task<SupplierReturn?> GetForUpdateAsync(Guid returnId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>Tracks a new return and gives it its id, which its stock document will refer to.</summary>
    void Add(SupplierReturn supplierReturn);

    Task<Result> SaveChangesAsync(CancellationToken ct);
}
