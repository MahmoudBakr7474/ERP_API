namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;

/// <param name="PaymentTermDays">Days from invoice date to due date that new orders start with.</param>
public sealed record SupplierState(Guid Id, string Code, string NameAr, bool IsActive, int PaymentTermDays);
