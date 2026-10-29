using WarehouseERP.Sales.Application.Contracts.Deliveries;

namespace WarehouseERP.Sales.Application.Features.Deliveries.UpdateSalesDelivery;

/// <summary>Replaces a draft delivery's picks and notes.</summary>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdateSalesDeliveryCommand(Guid DeliveryId, string? Notes, IReadOnlyList<DeliveryLineRequest> Lines, string RowVersion);
