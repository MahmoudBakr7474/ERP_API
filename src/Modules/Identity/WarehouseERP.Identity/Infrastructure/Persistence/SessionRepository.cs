using Microsoft.EntityFrameworkCore;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Application.Contracts.Sessions;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

internal sealed class SessionRepository(DbContext db, TimeProvider clock) : ISessionRepository
{
    private const int ListLimit = 50;

    public void Add(UserSession session) => db.Set<UserSession>().Add(session);

    public Task<UserSession?> GetAsync(Guid sessionId, CancellationToken ct) =>
        db.Set<UserSession>().SingleOrDefaultAsync(s => s.Id == sessionId, ct);

    public Task<UserSession?> FindByRefreshHashAsync(byte[] refreshHash, CancellationToken ct) =>
        db.Set<UserSession>().FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshHash || s.PreviousRefreshTokenHash == refreshHash, ct);

    public async Task<IReadOnlyList<UserSession>> GetActiveAsync(Guid userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return await db.Set<UserSession>()
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null && s.ExpiresAtUtc > now)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SessionResponse>> ListAsync(Guid userId, CancellationToken ct) =>
        await db.Set<UserSession>().AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastSeenAtUtc).ThenBy(s => s.Id)
            .Take(ListLimit)
            .Select(s => new SessionResponse(
                s.Id, s.ClientType, s.DeviceId, s.CreatedAtUtc, s.LastSeenAtUtc, s.ExpiresAtUtc, s.RevokedAtUtc, s.RevokedReason))
            .ToListAsync(ct);
}
