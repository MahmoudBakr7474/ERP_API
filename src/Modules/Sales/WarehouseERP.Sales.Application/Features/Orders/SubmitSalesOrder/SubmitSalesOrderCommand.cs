namespace WarehouseERP.Sales.Application.Features.Orders.SubmitSalesOrder;

public sealed record SubmitSalesOrderCommand(Guid OrderId, string RowVersion);
