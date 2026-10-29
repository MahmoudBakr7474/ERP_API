using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Contracts.Quotations;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Quotations.ListSalesQuotations;

// Quotations of the warehouses in the user's scope, newest first.
public sealed class ListSalesQuotationsHandler(
    ISalesQuotationReadStore quotations, SalesReferences references, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListSalesQuotationsQuery, PagedResult<SalesQuotationSummaryResponse>>
{
    public async Task<Result<PagedResult<SalesQuotationSummaryResponse>>> HandleAsync(ListSalesQuotationsQuery query, CancellationToken ct)
    {
        var today = await references.TodayAsync(ct);
        if (today.IsFailure)
            return today.Error;
        return await quotations.ListAsync(
            new SalesQuotationFilter(DocumentAccess.ScopeOf(currentUser), query.WarehouseId, query.CustomerId, query.Status,
                Text.Optional(query.Search)),
            today.Value, query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
