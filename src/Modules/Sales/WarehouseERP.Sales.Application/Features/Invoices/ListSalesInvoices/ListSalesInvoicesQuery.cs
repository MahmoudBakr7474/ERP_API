using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Features.Invoices.ListSalesInvoices;

/// <param name="Search">Matches the number, the draft reference or the customer's reference.</param>
public sealed record ListSalesInvoicesQuery(
    int Page, int? PageSize, Guid? WarehouseId, Guid? OrderId, Guid? CustomerId, SalesInvoiceStatus? Status, string? Search) : IPagedQuery;
