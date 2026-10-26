using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Authentication.Login;

// Browser sign-in: a server-side session whose random token the endpoint puts in an HttpOnly cookie.
public sealed partial class LoginHandler(
    IUserCredentials credentials, ISessionRepository sessions, IIdentityPersistence persistence,
    IOptions<IdentitySessionOptions> options, TimeProvider clock, ILogger<LoginHandler> logger)
    : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> HandleAsync(LoginCommand command, CancellationToken ct)
    {
        var verified = await Credentials.VerifyAsync(credentials, persistence, logger, command.UserName, command.Password, ct);
        if (verified.IsFailure)
            return verified.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        var (token, hash) = SessionTokens.Create();
        var session = UserSession.StartWeb(verified.Value, hash, now, options.Value.WebAbsoluteLifetime);
        sessions.Add(session);
        var saved = await persistence.SaveChangesAsync(ct); // the session and the reset failed-attempt count
        if (saved.IsFailure)
            return saved.Error;

        SignedIn(logger, verified.Value, session.Id);
        return new LoginResponse(verified.Value, session.Id, token, session.ExpiresAtUtc);
    }

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "User {UserId} signed in (web session {SessionId})")]
    private static partial void SignedIn(ILogger logger, Guid userId, Guid sessionId);
}
