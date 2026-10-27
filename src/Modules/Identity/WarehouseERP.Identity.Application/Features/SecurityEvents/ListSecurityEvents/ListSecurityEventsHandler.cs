using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Contracts.SecurityEvents;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.SecurityEvents.ListSecurityEvents;

// The audit trail for security officers: newest first, filtered by user, type and period.
public sealed class ListSecurityEventsHandler(ISecurityEventReadStore events, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListSecurityEventsQuery, PagedResult<SecurityEventResponse>>
{
    public async Task<Result<PagedResult<SecurityEventResponse>>> HandleAsync(ListSecurityEventsQuery query, CancellationToken ct) =>
        await events.ListAsync(
            new SecurityEventFilter(query.UserId, query.Type, query.FromUtc, query.ToUtc),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
