using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Application.Shared;

// Used by the Account, Sessions and Users areas.
internal static class SessionRevocation
{
    /// <summary>Ends every active session of a user, optionally keeping one (the caller's own).</summary>
    public static async Task RevokeAllAsync(
        ISessionRepository sessions, Guid userId, SessionRevokeReason reason, DateTime now, CancellationToken ct, Guid? keep = null)
    {
        foreach (var session in await sessions.GetActiveAsync(userId, ct))
            if (session.Id != keep)
                session.Revoke(reason, now);
    }
}
