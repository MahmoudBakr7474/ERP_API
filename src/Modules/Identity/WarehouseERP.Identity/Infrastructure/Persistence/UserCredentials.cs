using Microsoft.AspNetCore.Identity;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

// IUserCredentials over ASP.NET Core Identity's UserManager: password checks, lockout, password changes. Tracked
// only, like every Identity store here; the use case saves.
internal sealed class UserCredentials(UserManager<ApplicationUser> users) : IUserCredentials
{
    // Verified against when the user is unknown or locked out, so every refused login costs one hash check and
    // response time doesn't tell which user names exist.
    private static string? s_timingHash;

    public async Task<LoginCheck> CheckPasswordAsync(string userName, string password, CancellationToken ct)
    {
        var user = await users.FindByNameAsync(userName);
        if (user is null)
        {
            SpendHashTime(password);
            return new LoginCheck(LoginOutcome.UnknownUser, null);
        }
        if (await users.IsLockedOutAsync(user))
        {
            SpendHashTime(password);
            return new LoginCheck(LoginOutcome.LockedOut, user.Id);
        }
        if (!await users.CheckPasswordAsync(user, password))
        {
            await users.AccessFailedAsync(user); // may start a lockout
            return new LoginCheck(LoginOutcome.WrongPassword, user.Id);
        }
        if (!user.IsActive)
            return new LoginCheck(LoginOutcome.Inactive, user.Id);

        await users.ResetAccessFailedCountAsync(user);
        return new LoginCheck(LoginOutcome.Succeeded, user.Id);
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct)
    {
        if (await users.FindByIdAsync(userId.ToString()) is not { } user)
            return UserErrors.NotFound;
        var changed = await users.ChangePasswordAsync(user, currentPassword, newPassword);
        if (changed.Succeeded)
            return Result.Success();
        if (changed.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
        {
            await users.AccessFailedAsync(user);
            return AuthenticationErrors.PasswordIncorrect;
        }
        return IdentityResults.ToError(changed, "NewPassword");
    }

    public async Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct)
    {
        if (await users.FindByIdAsync(userId.ToString()) is not { } user)
            return UserErrors.NotFound;
        // No reset token: an administrator sets the password directly. Validation runs in AddPasswordAsync; on a
        // failure nothing is saved, so the removed hash never reaches the database.
        var removed = await users.RemovePasswordAsync(user);
        if (!removed.Succeeded)
            return IdentityResults.ToError(removed, "NewPassword");
        var added = await users.AddPasswordAsync(user, newPassword);
        if (!added.Succeeded)
            return IdentityResults.ToError(added, "NewPassword");
        // A new password is a fresh start: a lockout from earlier guesses no longer applies.
        await users.SetLockoutEndDateAsync(user, null);
        await users.ResetAccessFailedCountAsync(user);
        return Result.Success();
    }

    public async Task<Result> UnlockAsync(Guid userId, CancellationToken ct)
    {
        if (await users.FindByIdAsync(userId.ToString()) is not { } user)
            return UserErrors.NotFound;
        var unlocked = await users.SetLockoutEndDateAsync(user, null);
        return unlocked.Succeeded
            ? IdentityResults.ToResult(await users.ResetAccessFailedCountAsync(user))
            : IdentityResults.ToResult(unlocked);
    }

    private void SpendHashTime(string password)
    {
        var hash = s_timingHash ??= users.PasswordHasher.HashPassword(new ApplicationUser(), Guid.NewGuid().ToString());
        users.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), hash, password);
    }
}
