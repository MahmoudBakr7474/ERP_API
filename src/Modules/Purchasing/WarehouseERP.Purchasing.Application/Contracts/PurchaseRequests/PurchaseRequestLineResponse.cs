namespace WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;

/// <param name="OrderedQuantity">What orders that are not cancelled took from the line.</param>
/// <param name="RemainingQuantity">What may still be ordered while the request is approved.</param>
public sealed record PurchaseRequestLineResponse(
    Guid Id, int LineNumber, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId, string UnitCode,
    decimal Quantity, decimal OrderedQuantity, decimal RemainingQuantity, string? Notes);
