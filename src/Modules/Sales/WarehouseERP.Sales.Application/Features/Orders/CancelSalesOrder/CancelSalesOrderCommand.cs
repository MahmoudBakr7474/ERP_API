namespace WarehouseERP.Sales.Application.Features.Orders.CancelSalesOrder;

/// <param name="Reason">Optional; kept in its history and the reservation's.</param>
public sealed record CancelSalesOrderCommand(Guid OrderId, string? Reason, string RowVersion);
