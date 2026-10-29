namespace WarehouseERP.Sales.Application.Features.Orders.ReduceSalesOrderLine;

/// <summary>Takes part of a confirmed order line off the order and releases its reservation.</summary>
/// <param name="Quantity">In the line's unit; at most what remains to deliver.</param>
/// <param name="Reason">Why; kept in the order's and the reservation's history.</param>
public sealed record ReduceSalesOrderLineCommand(Guid OrderId, Guid LineId, decimal Quantity, string Reason, string RowVersion);
