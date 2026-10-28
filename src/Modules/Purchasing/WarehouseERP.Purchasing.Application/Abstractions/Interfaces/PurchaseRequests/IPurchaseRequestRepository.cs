using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;

/// <summary>Write side of purchase requests with their lines and history.</summary>
public interface IPurchaseRequestRepository
{
    Task<Guid?> FindWarehouseAsync(Guid requestId, CancellationToken ct);

    /// <summary>
    /// The request with its lines, tracked and locked against other writers until the transaction ends, or null. With a
    /// row version, the save fails with a conflict unless the request still has it.
    /// </summary>
    Task<PurchaseRequest?> GetForUpdateAsync(Guid requestId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>
    /// The requests holding these lines, tracked and locked in id order (so two orders drawing on the same requests wait
    /// for each other instead of deadlocking). Lines that don't exist are left out.
    /// </summary>
    Task<IReadOnlyList<PurchaseRequest>> GetByLinesForUpdateAsync(IReadOnlyCollection<Guid> requestLineIds, CancellationToken ct);

    void Add(PurchaseRequest request);

    Task<Result> SaveChangesAsync(CancellationToken ct);
}
