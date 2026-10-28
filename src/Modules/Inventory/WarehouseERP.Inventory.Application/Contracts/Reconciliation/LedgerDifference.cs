namespace WarehouseERP.Inventory.Application.Contracts.Reconciliation;

public sealed record LedgerDifference(
    Guid ProductId, string Sku, Guid WarehouseId, string WarehouseCode, Guid LocationId, string LocationCode,
    decimal MovementsQuantity, decimal BalanceQuantity);
