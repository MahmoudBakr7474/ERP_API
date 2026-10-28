using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.ListSupplierInvoices;

/// <param name="Search">Matches the supplier's invoice number, the company number or the draft reference.</param>
/// <param name="DueOnOrBefore">Posted invoices due by this date.</param>
public sealed record ListSupplierInvoicesQuery(
    int Page, int? PageSize, Guid? SupplierId, Guid? PurchaseOrderId, SupplierInvoiceStatus? Status, string? Search,
    DateOnly? DueOnOrBefore) : IPagedQuery;
