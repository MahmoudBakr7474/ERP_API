using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.ListSupplierReturns;

/// <param name="WithCreditClaim">Only posted returns with (true) or without (false) a pending credit claim.</param>
/// <param name="Search">Matches the number or the draft reference.</param>
public sealed record ListSupplierReturnsQuery(
    int Page, int? PageSize, Guid? SupplierId, Guid? PurchaseOrderId, Guid? GoodsReceiptId, SupplierReturnStatus? Status,
    bool? WithCreditClaim, string? Search) : IPagedQuery;
