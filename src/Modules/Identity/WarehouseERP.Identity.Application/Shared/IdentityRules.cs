using FluentValidation;
using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Shared;

// Input shape shared by several features. Password strength is ASP.NET Core Identity's policy (Identity:Password),
// reported as validation errors by IUserAccounts.
internal static class IdentityRules
{
    public static IRuleBuilderOptions<T, string> ValidUserName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(256).Matches("^[A-Za-z0-9._@-]+$");

    public static IRuleBuilderOptions<T, string> ValidDisplayName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(150);

    public static IRuleBuilderOptions<T, string?> ValidOptionalEmail<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(256).EmailAddress();

    public static IRuleBuilderOptions<T, string> PresentPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(256);

    public static IRuleBuilderOptions<T, string> ValidRoleName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(100);
}

internal static class PermissionCatalog
{
    /// <summary>Every permission name registered by the modules.</summary>
    public static IReadOnlySet<string> Names(IEnumerable<PermissionDefinition> catalog) =>
        catalog.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
}

// Verifies a user name and password for Login and IssueApiToken. Every failure becomes the same error; the reason
// is logged, and a wrong password's lockout count is saved even though the login fails. A conflict saving that
// count (parallel attempts on one account) still answers LoginFailed: only existing accounts have a count to save,
// so a different answer would reveal the user name.
internal static partial class Credentials
{
    public static async Task<Result<Guid>> VerifyAsync(
        IUserAccounts accounts, IIdentityPersistence persistence, ILogger logger, string userName, string password, CancellationToken ct)
    {
        var check = await accounts.CheckPasswordAsync(userName.Trim(), password, ct);
        if (check.Outcome == LoginOutcome.Succeeded)
            return check.UserId!.Value;

        _ = await persistence.SaveChangesAsync(ct); // the failed-attempt count
        if (check.UserId is { } userId)
            LoginRefused(logger, userId, check.Outcome);
        else
            LoginRefusedUnknownUser(logger);
        return IdentityApplicationErrors.LoginFailed;
    }

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Login refused for user {UserId}: {Outcome}")]
    private static partial void LoginRefused(ILogger logger, Guid userId, LoginOutcome outcome);

    // The typed user name is not logged: it may be a password typed in the wrong field.
    [LoggerMessage(EventId = 4003, Level = LogLevel.Warning, Message = "Login refused: unknown user name")]
    private static partial void LoginRefusedUnknownUser(ILogger logger);
}

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

// Someone must always be able to manage users: the last active Administrator can't lose the role or be deactivated.
internal static class AdministratorGuard
{
    public static async Task<Result> EnsureAnotherAdministratorAsync(
        IRoleRepository roles, IUserAccounts accounts, Guid userId, CancellationToken ct)
    {
        if (await roles.FindAdministratorIdAsync(ct) is not { } adminRoleId)
            return Result.Success();
        if (!(await accounts.GetRoleIdsAsync(userId, ct)).Contains(adminRoleId))
            return Result.Success();
        return await accounts.CountActiveUsersInRoleAsync(adminRoleId, excludingUserId: userId, ct) > 0
            ? Result.Success()
            : IdentityApplicationErrors.LastAdministrator;
    }
}
