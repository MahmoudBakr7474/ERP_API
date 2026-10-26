using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Identity.Application.Contracts.Users;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;

/// <summary>Read side of users: projections, no tracking.</summary>
public interface IUserReadStore
{
    Task<UserDetailsResponse?> GetAsync(Guid userId, CancellationToken ct);

    Task<PagedResult<UserSummaryResponse>> ListAsync(string? search, int page, int pageSize, CancellationToken ct);
}
