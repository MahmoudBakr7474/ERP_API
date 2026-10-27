using WarehouseERP.Identity.Application.Contracts.Sessions;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;

/// <summary>Server-side sessions.</summary>
public interface ISessionRepository
{
    void Add(UserSession session);

    Task<UserSession?> GetAsync(Guid sessionId, CancellationToken ct);

    /// <summary>The session whose current or previous refresh token has this hash.</summary>
    Task<UserSession?> FindByRefreshHashAsync(byte[] refreshHash, CancellationToken ct);

    /// <summary>The session that already exchanged a refresh token with this hash, at any earlier rotation.</summary>
    Task<UserSession?> FindByRetiredRefreshHashAsync(byte[] refreshHash, CancellationToken ct);

    /// <summary>Active sessions of a user, tracked, to end them.</summary>
    Task<IReadOnlyList<UserSession>> GetActiveAsync(Guid userId, CancellationToken ct);

    Task<IReadOnlyList<SessionResponse>> ListAsync(Guid userId, CancellationToken ct);
}
