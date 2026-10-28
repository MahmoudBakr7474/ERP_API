using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
/// <param name="WithCreditClaim">Only posted returns with (true) or without (false) a pending credit claim.</param>
public sealed record SupplierReturnFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? SupplierId, Guid? PurchaseOrderId, Guid? GoodsReceiptId,
    SupplierReturnStatus? Status, bool? WithCreditClaim, string? Search);
