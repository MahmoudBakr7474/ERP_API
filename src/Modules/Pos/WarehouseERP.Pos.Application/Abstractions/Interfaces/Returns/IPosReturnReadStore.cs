using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Pos.Application.Contracts.Returns;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Returns;

public interface IPosReturnReadStore
{
    /// <summary>The return as its receipt prints it.</summary>
    Task<PosReturnResponse?> GetAsync(Guid returnId, CancellationToken ct);

    /// <summary>Returns, most recent first, within the scope when one is given.</summary>
    Task<PagedResult<PosReturnSummaryResponse>> ListAsync(PosReturnFilter filter, int page, int pageSize, CancellationToken ct);
}
