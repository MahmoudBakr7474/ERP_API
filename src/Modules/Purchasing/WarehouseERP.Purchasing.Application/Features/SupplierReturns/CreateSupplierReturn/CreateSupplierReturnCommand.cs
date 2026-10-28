using WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.CreateSupplierReturn;

/// <param name="GoodsReceiptId">The posted goods receipt the goods came in on.</param>
/// <param name="Reason">Why the goods go back: damaged, wrong item, expired …</param>
public sealed record CreateSupplierReturnCommand(Guid GoodsReceiptId, string Reason, string? Notes, IReadOnlyList<ReturnLineRequest> Lines);
