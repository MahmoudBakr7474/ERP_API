namespace WarehouseERP.Sales.Application.Contracts.Customers;

/// <summary>A contact person at the customer; at most one is primary.</summary>
public sealed record CustomerContactRequest(string Name, string? JobTitle, string? Phone, string? Email, bool IsPrimary = false);
