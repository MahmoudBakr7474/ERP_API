using Microsoft.Extensions.Logging;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Authentication;

// Verifies a user name and password for Login and IssueApiToken. Every failure becomes the same error; the reason
// is logged, and a wrong password's lockout count is saved even though the login fails. A conflict saving that
// count (parallel attempts on one account) still answers LoginFailed: only existing accounts have a count to save,
// so a different answer would reveal the user name.
internal static partial class Credentials
{
    public static async Task<Result<Guid>> VerifyAsync(
        IUserCredentials credentials, IIdentityPersistence persistence, ILogger logger, string userName, string password, CancellationToken ct)
    {
        var check = await credentials.CheckPasswordAsync(userName.Trim(), password, ct);
        if (check.Outcome == LoginOutcome.Succeeded)
            return check.UserId!.Value;

        _ = await persistence.SaveChangesAsync(ct); // the failed-attempt count
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
