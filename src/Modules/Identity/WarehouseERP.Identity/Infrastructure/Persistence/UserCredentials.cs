using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

// IUserCredentials over ASP.NET Core Identity's UserManager: password checks, lockout, password changes. Tracked
// only, like every Identity store here; the use case saves. The one exception is the failed-attempt count and
// lockout: they are written at once with atomic UPDATEs (like a session's last-seen time), so parallel guesses each
// count and a refused attempt never waits for, or depends on, the use case's save. No transaction spans a login:
// each UPDATE commits on its own, before and apart from the use case's SaveChanges, so a counted guess stays counted
// even if saving its security events fails. Every UPDATE renews ConcurrencyStamp, so a user row loaded before it
// can't be saved over it (409); the tracked user of the request that made the UPDATE is kept in step instead.
internal sealed class UserCredentials(
    UserManager<ApplicationUser> users, DbContext db, IOptions<IdentityOptions> identityOptions, TimeProvider clock)
    : IUserCredentials
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
            return new LoginCheck(LoginOutcome.WrongPassword, user.Id, await RecordFailureAsync(user));
        if (!user.IsActive)
        {
            // A refused sign-in saves only its security events: a rehash of the right password (pending on the
            // tracked user) waits for a real sign-in instead of riding along with them.
            db.Entry(user).State = EntityState.Detached;
            return new LoginCheck(LoginOutcome.Inactive, user.Id);
        }

        // A right password may have been rehashed above: the tracked user then holds a pending update with a new
        // stamp, which the use case's save writes after this reset.
        if (user.AccessFailedCount > 0)
            await ResetFailuresAsync(user);
        return new LoginCheck(LoginOutcome.Succeeded, user.Id);
    }

    public async Task<PasswordChange> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct)
    {
        if (await users.FindByIdAsync(userId.ToString()) is not { } user)
            return new PasswordChange(UserErrors.NotFound);
        // Lockout guards the current password here too: a stolen session must not guess it without limit.
        if (await users.IsLockedOutAsync(user))
            return new PasswordChange(AuthenticationErrors.PasswordLockedOut);
        var changed = await users.ChangePasswordAsync(user, currentPassword, newPassword);
        if (changed.Succeeded)
            return new PasswordChange(Result.Success());
        if (changed.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
            return new PasswordChange(AuthenticationErrors.PasswordIncorrect, await RecordFailureAsync(user));
        return new PasswordChange(IdentityResults.ToError(changed, "NewPassword"));
    }

    public async Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct)
    {
        if (await users.FindByIdAsync(userId.ToString()) is not { } user)
            return UserErrors.NotFound;
        var loadedStamp = LoadedStamp(user);
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
        SaveAgainstLoadedStamp(user, loadedStamp);
        return Result.Success();
    }

    public async Task<Result> UnlockAsync(Guid userId, CancellationToken ct)
    {
        if (await users.FindByIdAsync(userId.ToString()) is not { } user)
            return UserErrors.NotFound;
        var loadedStamp = LoadedStamp(user);
        var unlocked = await users.SetLockoutEndDateAsync(user, null);
        if (!unlocked.Succeeded)
            return IdentityResults.ToResult(unlocked);
        var reset = await users.ResetAccessFailedCountAsync(user);
        SaveAgainstLoadedStamp(user, loadedStamp);
        return IdentityResults.ToResult(reset);
    }

    // Several UserManager steps on one user (reset, unlock) each end in UserStore.UpdateAsync, which re-attaches the
    // tracked user: EF Core then takes the previous step's unsaved stamp as the original value, so the use case's
    // single save would look for a stamp that was never stored and fail every time (409). The steps are this use
    // case's own change, so its save is checked against the stamp that was actually loaded, which still refuses a
    // change another request made since.
    private string? LoadedStamp(ApplicationUser user) => db.Entry(user).Property(u => u.ConcurrencyStamp).OriginalValue;

    private void SaveAgainstLoadedStamp(ApplicationUser user, string? loadedStamp) =>
        db.Entry(user).Property(u => u.ConcurrencyStamp).OriginalValue = loadedStamp;

    // UserManager.AccessFailedAsync without its read-modify-write through the tracked user, which lets parallel
    // guesses overwrite each other's count. At the threshold the lockout starts and the count resets, otherwise the
    // count grows. Returns whether this attempt started the lockout. Once a password has been checked, its outcome is
    // written whatever the client does: these UPDATEs ignore the request's cancellation, so an aborted request can't
    // skip or half-write its count.
    private async Task<bool> RecordFailureAsync(ApplicationUser user)
    {
        if (!user.LockoutEnabled)
            return false;
        var lockout = identityOptions.Value.Lockout;
        var threshold = lockout.MaxFailedAccessAttempts;
        DateTimeOffset? lockedUntil = clock.GetUtcNow() + lockout.DefaultLockoutTimeSpan;

        // Usually the row is still as loaded, so the next count follows from the loaded one.
        var loaded = db.Entry(user);
        var count = loaded.Property(u => u.AccessFailedCount).OriginalValue + 1;
        var locks = count >= threshold;
        var lockoutEnd = locks ? lockedUntil : loaded.Property(u => u.LockoutEnd).OriginalValue;
        if (await TryUpdateAsLoadedAsync(user, locks ? 0 : count, lockoutEnd))
            return locks;

        // Another request changed the row since: count on whatever it holds now, with two conditional UPDATEs, each
        // atomic, where the one that applies (1 row) says what happened; when a parallel attempt moved the count
        // between the two, try again. The tracked user stays as loaded, so saving it later is refused (409) rather
        // than writing back over that request's change.
        var row = db.Set<ApplicationUser>().Where(u => u.Id == user.Id);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (await row.Where(u => u.AccessFailedCount + 1 >= threshold)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(u => u.LockoutEnd, lockedUntil)
                        .SetProperty(u => u.AccessFailedCount, 0)
                        .SetProperty(u => u.ConcurrencyStamp, Guid.NewGuid().ToString()), CancellationToken.None) == 1)
                return true;
            if (await row.Where(u => u.AccessFailedCount + 1 < threshold)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(u => u.AccessFailedCount, u => u.AccessFailedCount + 1)
                        .SetProperty(u => u.ConcurrencyStamp, Guid.NewGuid().ToString()), CancellationToken.None) == 1)
                return false;
        }
        return false;
    }

    // A right password clears the count; as in RecordFailureAsync, from the loaded row when it is unchanged, otherwise
    // over whatever another request wrote since, leaving the tracked user as loaded.
    private async Task ResetFailuresAsync(ApplicationUser user)
    {
        if (await TryUpdateAsLoadedAsync(user, 0, db.Entry(user).Property(u => u.LockoutEnd).OriginalValue))
            return;
        await db.Set<ApplicationUser>().Where(u => u.Id == user.Id && u.AccessFailedCount > 0)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.AccessFailedCount, 0)
                .SetProperty(u => u.ConcurrencyStamp, Guid.NewGuid().ToString()), CancellationToken.None);
    }

    // Writes the count and lockout only while the row still has the stamp this context loaded. Every write to a user
    // row renews the stamp (the store's tracked updates and these UPDATEs alike), so a match means no other request
    // has changed the row since, and what is written here is the only change on top of the loaded values. The
    // tracked user can then take it on: each written value becomes the original one, so this use case's own save of
    // the user checks against our stamp, while another request's later change still fails it. It becomes the current
    // value too, unless the use case changed that property itself and hasn't saved it yet (a rehash's new stamp):
    // that change stays pending. Returns false, changing nothing, when the stamp no longer matches.
    private async Task<bool> TryUpdateAsLoadedAsync(ApplicationUser user, int failedCount, DateTimeOffset? lockoutEnd)
    {
        var entry = db.Entry(user);
        var loadedStamp = entry.Property(u => u.ConcurrencyStamp).OriginalValue;
        var stamp = Guid.NewGuid().ToString();
        if (await db.Set<ApplicationUser>().Where(u => u.Id == user.Id && u.ConcurrencyStamp == loadedStamp)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.AccessFailedCount, failedCount)
                    .SetProperty(u => u.LockoutEnd, lockoutEnd)
                    .SetProperty(u => u.ConcurrencyStamp, stamp), CancellationToken.None) == 0)
            return false;

        // Current values first, through the entity: a changed original value is then compared with them, so nothing
        // is marked modified that isn't (a needless whole-row save would also stamp UpdatedAtUtc).
        if (Unchanged(entry.Property(u => u.ConcurrencyStamp)))
            user.ConcurrencyStamp = stamp;
        if (Unchanged(entry.Property(u => u.AccessFailedCount)))
            user.AccessFailedCount = failedCount;
        if (Unchanged(entry.Property(u => u.LockoutEnd)))
            user.LockoutEnd = lockoutEnd;
        entry.Property(u => u.ConcurrencyStamp).OriginalValue = stamp;
        entry.Property(u => u.AccessFailedCount).OriginalValue = failedCount;
        entry.Property(u => u.LockoutEnd).OriginalValue = lockoutEnd;
        return true;

        static bool Unchanged<T>(PropertyEntry<ApplicationUser, T> property) =>
            EqualityComparer<T>.Default.Equals(property.CurrentValue, property.OriginalValue);
    }

    private void SpendHashTime(string password)
    {
        var hash = s_timingHash ??= users.PasswordHasher.HashPassword(new ApplicationUser(), Guid.NewGuid().ToString());
        users.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), hash, password);
    }
}
