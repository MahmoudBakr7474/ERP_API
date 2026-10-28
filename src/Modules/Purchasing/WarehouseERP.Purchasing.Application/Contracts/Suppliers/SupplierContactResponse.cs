namespace WarehouseERP.Purchasing.Application.Contracts.Suppliers;

public sealed record SupplierContactResponse(Guid Id, string Name, string? JobTitle, string? Phone, string? Email, bool IsPrimary);
