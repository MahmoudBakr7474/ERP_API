using Microsoft.Extensions.Logging;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Authentication;

// Verifies a user name and password for Login and IssueApiToken. Every failure becomes the same error; the reason
// is logged and recorded in the security audit trail. IUserCredentials writes a wrong password's lockout count at
// once (atomically, so parallel guesses each count); the save here only adds the events and its result is ignored,
// since any other answer than LoginFailed would reveal that the user name exists.
internal static partial class Credentials
{
    /// <param name="channel">"web" or "api", recorded with a refused attempt.</param>
    public static async Task<Result<Guid>> VerifyAsync(
        IUserCredentials credentials, ISecurityEventLog securityEvents, IIdentityPersistence persistence, ILogger logger,
        string userName, string password, string channel, CancellationToken ct)
    {
        var check = await credentials.CheckPasswordAsync(userName.Trim(), password, ct);
        if (check.Outcome == LoginOutcome.Succeeded)
            return check.UserId!.Value;

        // The typed user name is not recorded either; the reason is, for the security officer only.
        securityEvents.Record(SecurityEventType.SignInFailed, check.UserId, details: new { outcome = check.Outcome, channel });
        if (check.LockoutStarted)
            securityEvents.Record(SecurityEventType.UserLockedOut, check.UserId, details: new { channel });
        // The events only (the failed-attempt count is already written), and not with the request's token: the attempt
        // counted, so it is recorded even if the client left.
        _ = await persistence.SaveChangesAsync(CancellationToken.None);
        if (check.UserId is { } userId)
            LoginRefused(logger, userId, check.Outcome);
        else
            LoginRefusedUnknownUser(logger);
        return AuthenticationErrors.LoginFailed;
    }

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Login refused for user {UserId}: {Outcome}")]
    private static partial void LoginRefused(ILogger logger, Guid userId, LoginOutcome outcome);

    // The typed user name is not logged: it may be a password typed in the wrong field.
    [LoggerMessage(EventId = 4003, Level = LogLevel.Warning, Message = "Login refused: unknown user name")]
    private static partial void LoginRefusedUnknownUser(ILogger logger);
}
