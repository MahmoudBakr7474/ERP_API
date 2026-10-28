namespace WarehouseERP.Inventory.Application.Contracts.Reconciliation;

public sealed record AvailabilityDifference(
    Guid ProductId, string Sku, Guid WarehouseId, string WarehouseCode, decimal SellableBalance, decimal OnHandSellable);
