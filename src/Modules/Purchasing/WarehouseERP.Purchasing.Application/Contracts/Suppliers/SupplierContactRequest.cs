namespace WarehouseERP.Purchasing.Application.Contracts.Suppliers;

/// <summary>A person at the supplier, as the client sends it.</summary>
/// <param name="IsPrimary">The main contact; at most one.</param>
public sealed record SupplierContactRequest(string Name, string? JobTitle, string? Phone, string? Email, bool IsPrimary = false);
