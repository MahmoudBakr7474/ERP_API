namespace WarehouseERP.Inventory.Application.Contracts.Reconciliation;

public sealed record TransferDifference(
    Guid TransferId, string? Number, Guid ProductId, string Sku, decimal OutstandingQuantity, decimal TransitBalance);
