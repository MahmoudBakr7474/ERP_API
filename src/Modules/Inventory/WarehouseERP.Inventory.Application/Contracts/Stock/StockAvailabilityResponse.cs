namespace WarehouseERP.Inventory.Application.Contracts.Stock;

/// <param name="OnHandSellable">Available stock across the warehouse's locations.</param>
/// <param name="ReservedQuantity">Reserved by sales (0 until reservations exist).</param>
/// <param name="Available">On hand minus reserved; negative while over-reserved.</param>
public sealed record StockAvailabilityResponse(
    Guid ProductId, string Sku, string ProductNameAr, string BaseUnitCode, Guid WarehouseId, string WarehouseCode,
    decimal OnHandSellable, decimal ReservedQuantity, decimal Available);
