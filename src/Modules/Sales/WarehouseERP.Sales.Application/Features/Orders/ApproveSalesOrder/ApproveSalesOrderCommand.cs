namespace WarehouseERP.Sales.Application.Features.Orders.ApproveSalesOrder;

public sealed record ApproveSalesOrderCommand(Guid OrderId, string RowVersion);
