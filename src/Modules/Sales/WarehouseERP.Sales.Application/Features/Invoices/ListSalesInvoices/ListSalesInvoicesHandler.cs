using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Invoices;
using WarehouseERP.Sales.Application.Contracts.Invoices;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Invoices.ListSalesInvoices;

// Invoices of the warehouses in the user's scope, newest first.
public sealed class ListSalesInvoicesHandler(
    ISalesInvoiceReadStore invoices, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListSalesInvoicesQuery, PagedResult<SalesInvoiceSummaryResponse>>
{
    public async Task<Result<PagedResult<SalesInvoiceSummaryResponse>>> HandleAsync(ListSalesInvoicesQuery query, CancellationToken ct) =>
        await invoices.ListAsync(
            new SalesInvoiceFilter(DocumentAccess.ScopeOf(currentUser), query.WarehouseId, query.OrderId, query.CustomerId, query.Status,
                Text.Optional(query.Search)),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
