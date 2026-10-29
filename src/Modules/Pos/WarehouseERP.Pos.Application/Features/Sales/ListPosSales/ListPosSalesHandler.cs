using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Sales;
using WarehouseERP.Pos.Application.Contracts.Sales;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Sales.ListPosSales;

// The sales history of the user's warehouses; business dates are the company's.
public sealed class ListPosSalesHandler(
    IPosSaleReadStore sales, PosReferences references, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListPosSalesQuery, PagedResult<PosSaleSummaryResponse>>
{
    public async Task<Result<PagedResult<PosSaleSummaryResponse>>> HandleAsync(ListPosSalesQuery query, CancellationToken ct)
    {
        DateTime? from = null, before = null;
        if (query.From is not null || query.To is not null)
        {
            var company = await references.CompanyAsync(ct);
            if (company.IsFailure)
                return company.Error;
            from = query.From is { } start ? PosReferences.StartOf(start, company.Value) : null;
            before = query.To is { } end ? PosReferences.StartOf(end.AddDays(1), company.Value) : null;
        }
        return await sales.ListAsync(
            new PosSaleFilter(PosAccess.ScopeOf(currentUser), query.TerminalId, query.ShiftId, query.WarehouseId, query.CustomerId,
                query.CashierUserId, Text.Optional(query.Search), from, before),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
