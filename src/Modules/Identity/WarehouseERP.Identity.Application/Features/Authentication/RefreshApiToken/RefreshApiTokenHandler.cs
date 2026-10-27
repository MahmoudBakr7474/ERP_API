using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Contracts.Authentication;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Authentication.RefreshApiToken;

// Rotation: each refresh issues a new pair and retires the presented refresh token. Presenting a retired token (the
// previous one or any older one), or two refreshes racing with the same token, means a copy exists elsewhere, so the
// session ends; that revocation is saved and recorded before answering.
public sealed partial class RefreshApiTokenHandler(
    ISessionRepository sessions, IUserAccounts accounts, IIdentityPersistence persistence, IOptions<IdentitySessionOptions> options,
    TimeProvider clock, ILogger<RefreshApiTokenHandler> logger, ISecurityEventLog securityEvents)
    : ICommandHandler<RefreshApiTokenCommand, ApiTokensResponse>
{
    public async Task<Result<ApiTokensResponse>> HandleAsync(RefreshApiTokenCommand command, CancellationToken ct)
    {
        var presented = SessionTokens.Hash(command.RefreshToken);
        var now = clock.GetUtcNow().UtcDateTime;
        var session = await sessions.FindByRefreshHashAsync(presented, ct);
        if (session is null)
            return await sessions.FindByRetiredRefreshHashAsync(presented, ct) is { } reused
                ? await RejectReuseAsync(reused, now)
                : SessionDomainErrors.Revoked;
        if (HasEnded(session, now))
            return SessionDomainErrors.Revoked;

        // Requests of a deactivated user are refused already; ending the session here also stops one that slipped
        // past the deactivation from rotating on until the user is reactivated. Saved whatever the client does.
        if (await accounts.FindAsync(session.UserId, ct) is not { IsActive: true })
        {
            session.Revoke(SessionRevokeReason.UserDeactivated, now);
            securityEvents.Record(SecurityEventType.SessionRevoked, session.UserId, sessionId: session.Id,
                details: new { reason = SessionRevokeReason.UserDeactivated });
            var revoked = await persistence.SaveChangesAsync(CancellationToken.None);
            return revoked.IsFailure ? revoked.Error : SessionDomainErrors.Revoked;
        }

        var issued = ApiTokenIssuer.Issue(now, session.ExpiresAtUtc, options.Value);
        var refreshed = session.Refresh(presented, issued.Hashes, now);
        if (refreshed.IsFailure)
            return refreshed.Error == SessionDomainErrors.RefreshTokenReused
                ? await SaveReuseAsync(session)
                : refreshed.Error;

        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsSuccess)
            return issued.ToResponse(session.Id);
        if (saved.Error != CommonErrors.ConcurrencyConflict)
            return saved.Error;
        // Another request retired the same refresh token at the same moment (one row per retired hash): one of the
        // two callers holds a copy. Start again from the stored session and end it.
        persistence.DiscardChanges();
        return await sessions.GetAsync(session.Id, CancellationToken.None) is { } stored
            ? await RejectReuseAsync(stored, now)
            : SessionDomainErrors.Revoked;
    }

    // An ended session's tokens are all dead already, and its end was recorded when it happened: they answer Revoked,
    // as Refresh does, without another event or a warning about a revocation that isn't one.
    private static bool HasEnded(UserSession session, DateTime now) => session.IsRevoked || now >= session.ExpiresAtUtc;

    private Task<Result<ApiTokensResponse>> RejectReuseAsync(UserSession session, DateTime now)
    {
        if (HasEnded(session, now))
            return Task.FromResult<Result<ApiTokensResponse>>(SessionDomainErrors.Revoked);
        session.RejectRetiredRefreshToken(now);
        return SaveReuseAsync(session);
    }

    // Once a copy of a refresh token has shown up, ending the session must not depend on the client staying: the save
    // ignores the request's cancellation.
    private async Task<Result<ApiTokensResponse>> SaveReuseAsync(UserSession session)
    {
        securityEvents.Record(SecurityEventType.RefreshTokenReused, session.UserId, sessionId: session.Id);
        var saved = await persistence.SaveChangesAsync(CancellationToken.None);
        if (saved.IsFailure)
            return saved.Error;
        RefreshTokenReused(logger, session.Id, session.UserId);
        return SessionDomainErrors.RefreshTokenReused;
    }

    [LoggerMessage(EventId = 4005, Level = LogLevel.Warning,
        Message = "Refresh token reused on session {SessionId} of user {UserId}; session revoked")]
    private static partial void RefreshTokenReused(ILogger logger, Guid sessionId, Guid userId);
}
