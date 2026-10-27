using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Application.Features.SecurityEvents.ListSecurityEvents;

/// <summary>Bound straight from the query string (<c>?page&amp;pageSize&amp;userId&amp;type&amp;fromUtc&amp;toUtc</c>).</summary>
/// <param name="UserId">Events where this user acted or was the target.</param>
/// <param name="ToUtc">Exclusive end of the period.</param>
public sealed record ListSecurityEventsQuery(
    int Page = 1, int? PageSize = null, Guid? UserId = null, SecurityEventType? Type = null, DateTime? FromUtc = null, DateTime? ToUtc = null)
    : IPagedQuery;
