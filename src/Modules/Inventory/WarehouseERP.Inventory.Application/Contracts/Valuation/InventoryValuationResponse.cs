namespace WarehouseERP.Inventory.Application.Contracts.Valuation;

/// <summary>A product's company-wide valuation in the base currency (moving weighted average).</summary>
public sealed record InventoryValuationResponse(
    Guid ProductId, string Sku, string ProductNameAr, string BaseUnitCode, decimal TotalQuantity, decimal TotalValue,
    decimal AverageUnitCost, DateTime UpdatedAtUtc);
