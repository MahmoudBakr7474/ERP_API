using WarehouseERP.Sales.Application.Contracts.Documents;

namespace WarehouseERP.Sales.Application.Features.Orders.UpdateSalesOrder;

/// <summary>Replaces a draft order's customer, terms and lines; its warehouse stays.</summary>
/// <param name="ShippingAddressId">One of the customer's addresses; null for none.</param>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdateSalesOrderCommand(
    Guid OrderId, Guid CustomerId, DateOnly OrderDate, DateOnly? RequestedDeliveryDate, string? CustomerReference, int PaymentTermDays,
    Guid? ShippingAddressId, string? Notes, IReadOnlyList<SalesLineRequest> Lines, string RowVersion);
