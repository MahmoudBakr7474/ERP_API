namespace WarehouseERP.Sales.Application.Features.Orders.RejectSalesOrder;

/// <param name="Reason">Why it goes back to draft; kept in its history.</param>
public sealed record RejectSalesOrderCommand(Guid OrderId, string Reason, string RowVersion);
