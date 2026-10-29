using WarehouseERP.Sales.Application.Contracts.Documents;

namespace WarehouseERP.Sales.Presentation.Contracts.Orders;

/// <summary>A draft order's new customer, terms and lines; its warehouse stays.</summary>
/// <param name="ShippingAddressId">One of the customer's addresses; null for none.</param>
public sealed record UpdateSalesOrderRequest(
    Guid CustomerId, DateOnly OrderDate, DateOnly? RequestedDeliveryDate, string? CustomerReference, int PaymentTermDays,
    Guid? ShippingAddressId, string? Notes, IReadOnlyList<SalesLineRequest> Lines, string RowVersion);
