namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;

/// <param name="Search">Matches the code, either name or the tax registration number.</param>
public sealed record SupplierFilter(string? Search, bool? IsActive, string? CountryCode);
