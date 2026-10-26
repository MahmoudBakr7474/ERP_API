using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Application.Contracts.Authentication;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Authentication.IssueApiToken;

// Sign-in for the POS and API tools: a short bearer access token and a rotating refresh token on one session.
public sealed partial class IssueApiTokenHandler(
    IUserCredentials credentials, ISessionRepository sessions, IIdentityPersistence persistence,
    IOptions<IdentitySessionOptions> options, TimeProvider clock, ILogger<IssueApiTokenHandler> logger)
    : ICommandHandler<IssueApiTokenCommand, ApiTokensResponse>
{
    public async Task<Result<ApiTokensResponse>> HandleAsync(IssueApiTokenCommand command, CancellationToken ct)
    {
        var verified = await Credentials.VerifyAsync(credentials, persistence, logger, command.UserName, command.Password, ct);
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
