using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Pos.Application.Contracts.CardIncidents;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.CardIncidents;

public interface ICardIncidentReadStore
{
    /// <summary>Card incidents, most recent first, within the scope when one is given.</summary>
    Task<PagedResult<PosCardIncidentResponse>> ListAsync(CardIncidentFilter filter, int page, int pageSize, CancellationToken ct);
}
