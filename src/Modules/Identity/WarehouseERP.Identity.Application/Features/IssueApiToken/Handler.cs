using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.IssueApiToken;

// Sign-in for the POS and API tools: a short bearer access token and a rotating refresh token on one session.
public sealed partial class IssueApiTokenHandler(
    IUserAccounts accounts, ISessionRepository sessions, IIdentityPersistence persistence,
    IOptions<IdentitySessionOptions> options, TimeProvider clock, ILogger<IssueApiTokenHandler> logger)
    : ICommandHandler<IssueApiTokenCommand, ApiTokens>
{
    public async Task<Result<ApiTokens>> HandleAsync(IssueApiTokenCommand command, CancellationToken ct)
    {
        var verified = await Credentials.VerifyAsync(accounts, persistence, logger, command.UserName, command.Password, ct);
        if (verified.IsFailure)
            return verified.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        var settings = options.Value;
        var issued = ApiTokenIssuer.Issue(now, now + settings.ApiAbsoluteLifetime, settings);
        var session = UserSession.StartApi(verified.Value, command.DeviceId, issued.Hashes, now, settings.ApiAbsoluteLifetime);
        sessions.Add(session);
        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        TokenIssued(logger, verified.Value, session.Id);
        return issued.ToResponse(session.Id);
    }

    [LoggerMessage(EventId = 4004, Level = LogLevel.Information, Message = "User {UserId} signed in (API session {SessionId})")]
    private static partial void TokenIssued(ILogger logger, Guid userId, Guid sessionId);
}

internal sealed record IssuedPair(string AccessToken, string RefreshToken, IssuedApiTokens Hashes)
{
    public ApiTokens ToResponse(Guid sessionId) =>
        new(sessionId, AccessToken, Hashes.AccessExpiresAtUtc, RefreshToken, Hashes.RefreshExpiresAtUtc);
}

internal static class ApiTokenIssuer
{
    /// <summary>A new access/refresh pair; neither outlives the session.</summary>
    public static IssuedPair Issue(DateTime now, DateTime sessionExpiresAtUtc, IdentitySessionOptions settings)
    {
        var (access, accessHash) = SessionTokens.Create();
        var (refresh, refreshHash) = SessionTokens.Create();
        var accessExpires = Min(now + settings.ApiAccessTokenLifetime, sessionExpiresAtUtc);
        var refreshExpires = Min(now + settings.ApiRefreshTokenLifetime, sessionExpiresAtUtc);
        return new IssuedPair(access, refresh, new IssuedApiTokens(accessHash, accessExpires, refreshHash, refreshExpires));
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
