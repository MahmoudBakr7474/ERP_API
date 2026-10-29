namespace WarehouseERP.Sales.Domain;

/// <summary>A sales order's commercial terms besides its lines.</summary>
/// <param name="PaymentTermDays">Days from invoice date to due date for this order's invoices, 0 to 365.</param>
/// <param name="ShippingAddress">Where the goods go, as text taken from a customer address; editing the customer never changes it.</param>
public sealed record OrderTerms(
    Guid CustomerId, DateOnly OrderDate, DateOnly? RequestedDeliveryDate, string? CustomerReference, int PaymentTermDays,
    string? ShippingAddress, string? Notes);
