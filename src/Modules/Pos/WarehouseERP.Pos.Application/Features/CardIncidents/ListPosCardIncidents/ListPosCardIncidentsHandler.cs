using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.CardIncidents;
using WarehouseERP.Pos.Application.Contracts.CardIncidents;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.CardIncidents.ListPosCardIncidents;

// Card charges no sale was recorded for, in the user's warehouses: what to reverse on the card terminals.
public sealed class ListPosCardIncidentsHandler(
    ICardIncidentReadStore incidents, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListPosCardIncidentsQuery, PagedResult<PosCardIncidentResponse>>
{
    public async Task<Result<PagedResult<PosCardIncidentResponse>>> HandleAsync(ListPosCardIncidentsQuery query, CancellationToken ct) =>
        await incidents.ListAsync(
            new CardIncidentFilter(PosAccess.ScopeOf(currentUser), query.TerminalId, query.ShiftId, query.Status),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
