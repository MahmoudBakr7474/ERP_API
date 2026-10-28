namespace WarehouseERP.Inventory.Application.Contracts.Stock;

/// <summary>An authorized decrease left less sellable stock than is reserved; recorded, never corrected silently.</summary>
public sealed record OverReservationResponse(
    Guid ProductId, string Sku, Guid WarehouseId, string WarehouseCode, Guid DocumentId, string? DocumentNumber,
    decimal OnHandSellable, decimal ReservedQuantity, DateTime DetectedAtUtc);
