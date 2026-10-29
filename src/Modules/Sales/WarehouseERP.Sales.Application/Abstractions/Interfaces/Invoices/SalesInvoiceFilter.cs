using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
/// <param name="Search">Matches the number, the draft reference or the customer's reference.</param>
public sealed record SalesInvoiceFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? WarehouseId, Guid? OrderId, Guid? CustomerId, SalesInvoiceStatus? Status,
    string? Search);
