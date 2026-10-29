using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;
using WarehouseERP.Pos.Application.Contracts.Shifts;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Shifts.ListPosShifts;

public sealed class ListPosShiftsHandler(IPosShiftReadStore shifts, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListPosShiftsQuery, PagedResult<PosShiftSummaryResponse>>
{
    public async Task<Result<PagedResult<PosShiftSummaryResponse>>> HandleAsync(ListPosShiftsQuery query, CancellationToken ct) =>
        await shifts.ListAsync(
            new PosShiftFilter(PosAccess.ScopeOf(currentUser), query.TerminalId, query.WarehouseId, query.CashierUserId, query.Status),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
