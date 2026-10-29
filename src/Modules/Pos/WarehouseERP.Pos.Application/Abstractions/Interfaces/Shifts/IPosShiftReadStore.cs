using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Pos.Application.Contracts.Shifts;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;

public interface IPosShiftReadStore
{
    /// <summary>The shift report: totals, expected and counted cash, cash movements and history.</summary>
    Task<PosShiftResponse?> GetAsync(Guid shiftId, CancellationToken ct);

    /// <summary>The id of the user's open shift on the terminal, or null.</summary>
    Task<Guid?> FindOpenAsync(Guid terminalId, Guid cashierUserId, CancellationToken ct);

    /// <summary>Shifts, most recently opened first, within the scope when one is given.</summary>
    Task<PagedResult<PosShiftSummaryResponse>> ListAsync(PosShiftFilter filter, int page, int pageSize, CancellationToken ct);
}
