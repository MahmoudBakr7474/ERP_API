using WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.UpdatePurchaseOrder;

/// <summary>Replaces a draft's supplier, terms and lines; its warehouse stays.</summary>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdatePurchaseOrderCommand(
    Guid PurchaseOrderId, Guid SupplierId, DateOnly? ExpectedDeliveryDate, string? SupplierReference, int PaymentTermDays,
    string? Notes, IReadOnlyList<OrderLineRequest> Lines, string RowVersion);
