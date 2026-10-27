using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Identity.Application.Contracts.SecurityEvents;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;

/// <summary>Read side of the security audit trail: newest first, no tracking.</summary>
public interface ISecurityEventReadStore
{
    Task<PagedResult<SecurityEventResponse>> ListAsync(SecurityEventFilter filter, int page, int pageSize, CancellationToken ct);
}
