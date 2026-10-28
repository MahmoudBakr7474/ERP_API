namespace WarehouseERP.Purchasing.Domain;

/// <summary>A change in what is on order from a request line: positive to order, negative to give back.</summary>
/// <param name="OrderLineNumber">The order line that causes it, for error messages.</param>
public sealed record RequestAllocation(Guid RequestLineId, decimal Quantity, int OrderLineNumber);
