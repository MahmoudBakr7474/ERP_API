namespace WarehouseERP.Sales.Application.Contracts.Customers;

public sealed record CustomerContactResponse(Guid Id, string Name, string? JobTitle, string? Phone, string? Email, bool IsPrimary);
