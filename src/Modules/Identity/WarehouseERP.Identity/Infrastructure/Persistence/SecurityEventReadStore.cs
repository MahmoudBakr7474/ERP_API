using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Contracts.SecurityEvents;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

internal sealed class SecurityEventReadStore(DbContext db) : ISecurityEventReadStore
{
    public async Task<PagedResult<SecurityEventResponse>> ListAsync(SecurityEventFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<SecurityEvent>().AsNoTracking();
        if (filter.UserId is { } userId)
            query = query.Where(e => e.ActorUserId == userId || e.TargetUserId == userId);
        if (filter.Type is { } type)
            query = query.Where(e => e.Type == type);
        if (filter.FromUtc is { } from)
            query = query.Where(e => e.OccurredAtUtc >= from);
        if (filter.ToUtc is { } to)
            query = query.Where(e => e.OccurredAtUtc < to);

        var users = db.Set<ApplicationUser>();
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.OccurredAtUtc).ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new SecurityEventResponse(
                e.Id, e.OccurredAtUtc, e.Type,
                e.ActorUserId, users.Where(u => u.Id == e.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(),
                e.TargetUserId, users.Where(u => u.Id == e.TargetUserId).Select(u => u.DisplayName).FirstOrDefault(),
                e.RoleId, e.SessionId, e.IpAddress, e.Details))
            .ToListAsync(ct);
        return new PagedResult<SecurityEventResponse>(items, page, pageSize, total);
    }
}
