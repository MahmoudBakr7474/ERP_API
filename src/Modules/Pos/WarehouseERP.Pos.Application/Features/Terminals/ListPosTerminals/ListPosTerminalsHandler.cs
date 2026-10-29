using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Terminals;
using WarehouseERP.Pos.Application.Contracts.Terminals;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Terminals.ListPosTerminals;

// Terminals of the user's warehouses only; a cashier picks one here.
public sealed class ListPosTerminalsHandler(IPosTerminalReadStore terminals, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListPosTerminalsQuery, PagedResult<PosTerminalResponse>>
{
    public async Task<Result<PagedResult<PosTerminalResponse>>> HandleAsync(ListPosTerminalsQuery query, CancellationToken ct) =>
        await terminals.ListAsync(
            new PosTerminalFilter(PosAccess.ScopeOf(currentUser), query.WarehouseId, query.IsActive, Text.Optional(query.Search)),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
