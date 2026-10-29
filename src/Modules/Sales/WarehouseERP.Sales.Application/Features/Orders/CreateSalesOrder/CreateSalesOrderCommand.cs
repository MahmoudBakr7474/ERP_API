using WarehouseERP.Sales.Application.Contracts.Documents;

namespace WarehouseERP.Sales.Application.Features.Orders.CreateSalesOrder;

/// <param name="CustomerId">An active customer.</param>
/// <param name="WarehouseId">An active physical warehouse that allows selling, in the user's scope; fixed once created. Its branch numbers the order and its stock is reserved there.</param>
/// <param name="OrderDate">Today (company time zone) when left out.</param>
/// <param name="PaymentTermDays">The customer's terms when left out.</param>
/// <param name="ShippingAddressId">One of the customer's addresses; the order keeps it as text.</param>
public sealed record CreateSalesOrderCommand(
    Guid CustomerId, Guid WarehouseId, DateOnly? OrderDate, DateOnly? RequestedDeliveryDate, string? CustomerReference,
    int? PaymentTermDays, Guid? ShippingAddressId, string? Notes, IReadOnlyList<SalesLineRequest> Lines);
