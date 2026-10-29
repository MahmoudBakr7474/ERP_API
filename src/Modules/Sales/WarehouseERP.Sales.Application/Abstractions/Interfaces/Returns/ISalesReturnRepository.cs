using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;

/// <summary>Write side of customer returns with their lines and history (one aggregate).</summary>
public interface ISalesReturnRepository
{
    /// <summary>The return's warehouse, which decides who may see it; null when no return has this id.</summary>
    Task<Guid?> FindWarehouseAsync(Guid returnId, CancellationToken ct);

    /// <summary>
    /// The return with its lines, tracked and locked against other writers until the transaction ends, or null. With a row
    /// version, the save fails with a conflict unless the return still has it.
    /// </summary>
    Task<SalesReturn?> GetForUpdateAsync(Guid returnId, byte[]? expectedRowVersion, CancellationToken ct);

    void Add(SalesReturn salesReturn);

    /// <summary>The use case's save point; a return whose lines or history changed moves its row version too.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
