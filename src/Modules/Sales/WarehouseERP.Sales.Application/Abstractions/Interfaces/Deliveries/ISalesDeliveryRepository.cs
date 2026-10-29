using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;

/// <summary>Write side of deliveries with their lines and history (one aggregate).</summary>
public interface ISalesDeliveryRepository
{
    /// <summary>The delivery's warehouse, which decides who may see it; null when no delivery has this id.</summary>
    Task<Guid?> FindWarehouseAsync(Guid deliveryId, CancellationToken ct);

    /// <summary>
    /// The delivery with its lines, tracked and locked against other writers until the transaction ends (posting it and
    /// approving returns from it wait for each other here), or null. With a row version, the save fails with a conflict
    /// unless the delivery still has it.
    /// </summary>
    Task<SalesDelivery?> GetForUpdateAsync(Guid deliveryId, byte[]? expectedRowVersion, CancellationToken ct);

    void Add(SalesDelivery delivery);

    /// <summary>The use case's save point; a delivery whose lines or history changed moves its row version too.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
