using WarehouseERP.Sales.Application.Contracts.Deliveries;

namespace WarehouseERP.Sales.Presentation.Contracts.Deliveries;

/// <summary>A draft delivery's new picks and notes.</summary>
public sealed record UpdateSalesDeliveryRequest(string? Notes, IReadOnlyList<DeliveryLineRequest> Lines, string RowVersion);
