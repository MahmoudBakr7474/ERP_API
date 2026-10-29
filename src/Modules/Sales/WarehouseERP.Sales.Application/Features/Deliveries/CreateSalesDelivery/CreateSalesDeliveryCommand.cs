using WarehouseERP.Sales.Application.Contracts.Deliveries;

namespace WarehouseERP.Sales.Application.Features.Deliveries.CreateSalesDelivery;

/// <summary>A picking list for a confirmed order; nothing leaves stock until it is posted.</summary>
/// <param name="OrderId">A confirmed or partially delivered order in the user's scope.</param>
public sealed record CreateSalesDeliveryCommand(Guid OrderId, string? Notes, IReadOnlyList<DeliveryLineRequest> Lines);
