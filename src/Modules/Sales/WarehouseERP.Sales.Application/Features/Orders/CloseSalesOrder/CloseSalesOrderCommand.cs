namespace WarehouseERP.Sales.Application.Features.Orders.CloseSalesOrder;

/// <param name="Reason">Why what remains will not be delivered; kept on the order.</param>
public sealed record CloseSalesOrderCommand(Guid OrderId, string Reason, string RowVersion);
