using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Features.Transfers.ListStockTransfers;

/// <param name="WarehouseId">The source or the destination.</param>
/// <param name="Search">Matches the number, the reference or the external reference.</param>
public sealed record ListStockTransfersQuery(
    int Page, int? PageSize, Guid? WarehouseId = null, StockTransferStatus? Status = null, string? Search = null) : IPagedQuery;
