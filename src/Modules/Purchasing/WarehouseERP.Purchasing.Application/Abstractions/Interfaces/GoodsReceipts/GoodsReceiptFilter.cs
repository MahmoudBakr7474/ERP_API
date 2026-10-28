namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.GoodsReceipts;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
/// <param name="Search">Matches the receipt number, its stock document number or the supplier's delivery note.</param>
public sealed record GoodsReceiptFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? WarehouseId, Guid? SupplierId, Guid? PurchaseOrderId, string? Search);
