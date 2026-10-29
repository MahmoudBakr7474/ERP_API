using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Pos.Application.Contracts.Terminals;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Terminals;

public interface IPosTerminalReadStore
{
    Task<PosTerminalResponse?> GetAsync(Guid terminalId, CancellationToken ct);

    /// <summary>Terminals by code, within the scope when one is given.</summary>
    Task<PagedResult<PosTerminalResponse>> ListAsync(PosTerminalFilter filter, int page, int pageSize, CancellationToken ct);
}
