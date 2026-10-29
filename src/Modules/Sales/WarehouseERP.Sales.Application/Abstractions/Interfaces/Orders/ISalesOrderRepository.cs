using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;

/// <summary>Write side of sales orders with their lines and history (one aggregate).</summary>
public interface ISalesOrderRepository
{
    /// <summary>The order's warehouse, which decides who may see it; null when no order has this id.</summary>
    Task<Guid?> FindWarehouseAsync(Guid orderId, CancellationToken ct);

    /// <summary>
    /// The order with its lines, tracked and locked against other writers until the transaction ends (confirmation,
    /// deliveries, invoices, returns and reductions of one order wait for each other here), or null. With a row version,
    /// the save fails with a conflict unless the order still has it.
    /// </summary>
    Task<SalesOrder?> GetForUpdateAsync(Guid orderId, byte[]? expectedRowVersion, CancellationToken ct);

    void Add(SalesOrder order);

    /// <summary>The use case's save point; an order whose lines or history changed moves its row version too.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
