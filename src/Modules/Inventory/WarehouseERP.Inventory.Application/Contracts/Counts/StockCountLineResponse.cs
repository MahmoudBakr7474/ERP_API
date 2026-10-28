namespace WarehouseERP.Inventory.Application.Contracts.Counts;

/// <summary>One product in one location, in the product's base unit.</summary>
/// <param name="ExpectedQuantity">The frozen balance (zero for something found that was not expected).</param>
/// <param name="CountedQuantity">Null until counted.</param>
/// <param name="Variance">Counted minus expected; zero until counted.</param>
/// <param name="UnitCost">Cost of one base unit for a surplus, when given.</param>
public sealed record StockCountLineResponse(
    Guid Id, Guid ProductId, string Sku, string ProductNameAr, string BaseUnitCode, Guid LocationId, string LocationCode,
    decimal ExpectedQuantity, decimal? CountedQuantity, decimal Variance, decimal? UnitCost);
