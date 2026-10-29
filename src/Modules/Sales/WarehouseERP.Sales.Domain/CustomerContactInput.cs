namespace WarehouseERP.Sales.Domain;

/// <summary>One of the customer's contacts; at most one is primary.</summary>
public sealed record CustomerContactInput(string Name, string? JobTitle, string? Phone, string? Email, bool IsPrimary);
