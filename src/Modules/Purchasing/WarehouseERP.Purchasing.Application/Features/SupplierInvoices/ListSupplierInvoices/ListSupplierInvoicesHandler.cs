using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.ListSupplierInvoices;

// Invoices of orders in the user's warehouse scope, newest first.
public sealed class ListSupplierInvoicesHandler(
    ISupplierInvoiceReadStore invoices, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListSupplierInvoicesQuery, PagedResult<SupplierInvoiceSummaryResponse>>
{
    public async Task<Result<PagedResult<SupplierInvoiceSummaryResponse>>> HandleAsync(ListSupplierInvoicesQuery query, CancellationToken ct) =>
        await invoices.ListAsync(
            new SupplierInvoiceFilter(DocumentAccess.ScopeOf(currentUser), query.SupplierId, query.PurchaseOrderId, query.Status,
                Text.Optional(query.Search), query.DueOnOrBefore),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
