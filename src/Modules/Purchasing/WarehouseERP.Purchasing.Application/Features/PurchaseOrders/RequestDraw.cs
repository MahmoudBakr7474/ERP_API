namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders;

/// <summary>What one order line takes from a purchase request line, in that line's product unit.</summary>
internal sealed record RequestDraw(decimal Quantity, int OrderLineNumber, Guid ProductUnitId);
