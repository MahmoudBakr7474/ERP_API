using WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.CreatePurchaseOrder;

/// <param name="SupplierId">An active supplier.</param>
/// <param name="WarehouseId">An active physical warehouse in the user's scope; fixed once created. Its branch numbers the order.</param>
/// <param name="SupplierReference">The supplier's quotation or reference.</param>
/// <param name="PaymentTermDays">Days from invoice date to due date; the supplier's terms when left out.</param>
public sealed record CreatePurchaseOrderCommand(
    Guid SupplierId, Guid WarehouseId, DateOnly? ExpectedDeliveryDate, string? SupplierReference, int? PaymentTermDays,
    string? Notes, IReadOnlyList<OrderLineRequest> Lines);
