using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Application.Features.IssueApiToken;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.RefreshApiToken;

// Rotation: each refresh issues a new pair and kills the presented refresh token. Presenting a token that was
// already used means a copy exists elsewhere, so the session ends; that revocation is saved before answering.
public sealed partial class RefreshApiTokenHandler(
    ISessionRepository sessions, IIdentityPersistence persistence, IOptions<IdentitySessionOptions> options,
    TimeProvider clock, ILogger<RefreshApiTokenHandler> logger)
    : ICommandHandler<RefreshApiTokenCommand, ApiTokens>
{
    public async Task<Result<ApiTokens>> HandleAsync(RefreshApiTokenCommand command, CancellationToken ct)
    {
        var presented = SessionTokens.Hash(command.RefreshToken);
        var session = await sessions.FindByRefreshHashAsync(presented, ct);
        if (session is null)
            return IdentityErrors.SessionRevoked;

        var now = clock.GetUtcNow().UtcDateTime;
        var issued = ApiTokenIssuer.Issue(now, session.ExpiresAtUtc, options.Value);
        var refreshed = session.Refresh(presented, issued.Hashes, now);

        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        if (refreshed.IsFailure)
        {
            if (refreshed.Error == IdentityErrors.RefreshTokenReused)
                RefreshTokenReused(logger, session.Id, session.UserId);
            return refreshed.Error;
        }
        return issued.ToResponse(session.Id);
    }

    [LoggerMessage(EventId = 4005, Level = LogLevel.Warning,
        Message = "Refresh token reused on session {SessionId} of user {UserId}; session revoked")]
    private static partial void RefreshTokenReused(ILogger logger, Guid sessionId, Guid userId);
}
