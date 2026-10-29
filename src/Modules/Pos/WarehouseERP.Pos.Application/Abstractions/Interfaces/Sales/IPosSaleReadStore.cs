using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Pos.Application.Contracts.Sales;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Sales;

public interface IPosSaleReadStore
{
    /// <summary>The sale as its receipt prints it, with its returns.</summary>
    Task<PosSaleResponse?> GetAsync(Guid saleId, CancellationToken ct);

    /// <summary>Sales, most recent first, within the scope when one is given.</summary>
    Task<PagedResult<PosSaleSummaryResponse>> ListAsync(PosSaleFilter filter, int page, int pageSize, CancellationToken ct);
}
