namespace WarehouseERP.Sales.Presentation.Contracts.Orders;

/// <summary>How much of a confirmed line to take off, in its unit, and why.</summary>
public sealed record ReduceSalesOrderLineRequest(decimal Quantity, string Reason, string RowVersion);
