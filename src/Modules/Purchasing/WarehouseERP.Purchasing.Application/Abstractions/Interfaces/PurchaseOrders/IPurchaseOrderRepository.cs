using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;

/// <summary>Write side of purchase orders with their lines and history (one aggregate).</summary>
public interface IPurchaseOrderRepository
{
    /// <summary>The order's warehouse, which decides who may see it; null when no order has this id.</summary>
    Task<Guid?> FindWarehouseAsync(Guid orderId, CancellationToken ct);

    /// <summary>
    /// The order with its lines, tracked and locked against other writers until the transaction ends (receipts, invoices
    /// and returns of one order wait for each other here), or null. With a row version, the save fails with a conflict
    /// unless the order still has it.
    /// </summary>
    Task<PurchaseOrder?> GetForUpdateAsync(Guid orderId, byte[]? expectedRowVersion, CancellationToken ct);

    void Add(PurchaseOrder order);

    /// <summary>The use case's save point; an order whose lines or history changed moves its row version too.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
