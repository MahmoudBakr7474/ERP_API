using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for a company-wide user.</param>
/// <param name="Search">Matches the supplier's invoice number, the company number or the draft reference.</param>
/// <param name="DueOnOrBefore">Posted invoices due by this date (for future payables).</param>
public sealed record SupplierInvoiceFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? SupplierId, Guid? PurchaseOrderId, SupplierInvoiceStatus? Status,
    string? Search, DateOnly? DueOnOrBefore);
