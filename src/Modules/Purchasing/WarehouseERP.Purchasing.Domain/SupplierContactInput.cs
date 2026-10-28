namespace WarehouseERP.Purchasing.Domain;

/// <summary>A person to talk to at the supplier.</summary>
/// <param name="IsPrimary">The main contact; at most one.</param>
public sealed record SupplierContactInput(string Name, string? JobTitle, string? Phone, string? Email, bool IsPrimary);
