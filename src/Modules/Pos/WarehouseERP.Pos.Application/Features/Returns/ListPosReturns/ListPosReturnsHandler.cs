using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Pos.Application.Contracts.Returns;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Returns.ListPosReturns;

public sealed class ListPosReturnsHandler(
    IPosReturnReadStore returns, PosReferences references, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListPosReturnsQuery, PagedResult<PosReturnSummaryResponse>>
{
    public async Task<Result<PagedResult<PosReturnSummaryResponse>>> HandleAsync(ListPosReturnsQuery query, CancellationToken ct)
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
        return await returns.ListAsync(
            new PosReturnFilter(PosAccess.ScopeOf(currentUser), query.TerminalId, query.ShiftId, query.SaleId, query.WarehouseId,
                Text.Optional(query.Search), from, before),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
