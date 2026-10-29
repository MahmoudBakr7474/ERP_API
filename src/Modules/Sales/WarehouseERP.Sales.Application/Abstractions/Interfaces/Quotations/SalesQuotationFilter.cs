using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
/// <param name="Search">Matches the number, the draft reference or the customer's reference.</param>
public sealed record SalesQuotationFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? WarehouseId, Guid? CustomerId, SalesQuotationStatus? Status, string? Search);
