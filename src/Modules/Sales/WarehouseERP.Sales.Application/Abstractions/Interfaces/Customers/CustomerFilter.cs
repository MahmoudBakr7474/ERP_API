using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;

/// <param name="Search">Matches the code, either name, the mobile or phone, or the tax registration number.</param>
public sealed record CustomerFilter(string? Search, bool? IsActive, CustomerType? CustomerType, bool? IsOnCreditHold, string? CountryCode);
