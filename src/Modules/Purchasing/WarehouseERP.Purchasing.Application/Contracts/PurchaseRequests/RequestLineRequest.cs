namespace WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;

/// <summary>One line of a purchase request as the client sends it.</summary>
/// <param name="ProductId">An active stock item without batch or serial tracking.</param>
/// <param name="ProductUnitId">An active unit of the product; orders drawing on the line use the same unit.</param>
public sealed record RequestLineRequest(Guid ProductId, Guid ProductUnitId, decimal Quantity, string? Notes = null);
