namespace WarehouseERP.Inventory.Application.Contracts.Reconciliation;

/// <param name="StockQuantity">Every balance of the product, transit included.</param>
/// <param name="EntriesQuantity">The sum of its cost entries' quantity changes.</param>
/// <param name="EntriesValue">The sum of its cost entries' value changes.</param>
public sealed record ValuationDifference(
    Guid ProductId, string Sku, decimal StockQuantity, decimal ValuedQuantity, decimal EntriesQuantity, decimal ValuedValue,
    decimal EntriesValue);
