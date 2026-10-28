using WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;

namespace WarehouseERP.Purchasing.Presentation.Contracts.PurchaseOrders;

public sealed record UpdatePurchaseOrderRequest(
    Guid SupplierId, DateOnly? ExpectedDeliveryDate, string? SupplierReference, int PaymentTermDays, string? Notes,
    IReadOnlyList<OrderLineRequest> Lines, string RowVersion);
