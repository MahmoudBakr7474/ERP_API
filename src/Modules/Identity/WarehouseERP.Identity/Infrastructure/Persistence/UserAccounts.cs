using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Identity.Application;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

// IUserAccounts over ASP.NET Core Identity's UserManager. The store runs with AutoSaveChanges off, so nothing here
// writes: the use case's IIdentityPersistence.SaveChangesAsync commits everything at once. Role assignments go
// straight to identity.UserRoles by id (UserManager only knows roles by name).
internal sealed class UserAccounts(UserManager<ApplicationUser> users, DbContext db, TimeProvider clock) : IUserAccounts
{
    // Verified against when the user is unknown or locked out, so every refused login costs one hash check and
    // response time doesn't tell which user names exist.
    private static string? s_timingHash;

    public Task<UserAccount?> FindAsync(Guid userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        return db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserAccount(u.Id, u.UserName!, u.DisplayName, u.Email, u.IsActive, u.LockoutEnd != null && u.LockoutEnd > now))
            .SingleOrDefaultAsync(ct);
    }

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

    public async Task<Result<Guid>> CreateAsync(NewUserAccount account, CancellationToken ct)
    {
        var user = new ApplicationUser { UserName = account.UserName, Email = account.Email, DisplayName = account.DisplayName };
        var created = await users.CreateAsync(user, account.Password);
        return created.Succeeded ? user.Id : IdentityResults.ToError(created, nameof(NewUserAccount.Password));
    }

    public async Task<Result> UpdateProfileAsync(Guid userId, string displayName, string? email, CancellationToken ct)
    {
        if (await FindTrackedAsync(userId) is not { } user)
            return IdentityApplicationErrors.UserNotFound;
        user.DisplayName = displayName;
        user.Email = email;
        return Map(await users.UpdateAsync(user));
    }

    public async Task<Result> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct)
    {
        if (await FindTrackedAsync(userId) is not { } user)
            return IdentityApplicationErrors.UserNotFound;
        user.IsActive = isActive;
        return Map(isActive ? await users.UpdateAsync(user) : await users.UpdateSecurityStampAsync(user));
    }

    public async Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct)
    {
        if (await FindTrackedAsync(userId) is not { } user)
            return IdentityApplicationErrors.UserNotFound;
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

    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct)
    {
        if (await FindTrackedAsync(userId) is not { } user)
            return IdentityApplicationErrors.UserNotFound;
        var changed = await users.ChangePasswordAsync(user, currentPassword, newPassword);
        if (changed.Succeeded)
            return Result.Success();
        if (changed.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
        {
            await users.AccessFailedAsync(user);
            return IdentityApplicationErrors.PasswordIncorrect;
        }
        return IdentityResults.ToError(changed, "NewPassword");
    }

    public async Task<Result> UnlockAsync(Guid userId, CancellationToken ct)
    {
        if (await FindTrackedAsync(userId) is not { } user)
            return IdentityApplicationErrors.UserNotFound;
        var unlocked = await users.SetLockoutEndDateAsync(user, null);
        return unlocked.Succeeded ? Map(await users.ResetAccessFailedCountAsync(user)) : Map(unlocked);
    }

    public async Task<IReadOnlyList<Guid>> GetRoleIdsAsync(Guid userId, CancellationToken ct) =>
        await db.Set<IdentityUserRole<Guid>>().Where(x => x.UserId == userId).Select(x => x.RoleId).ToListAsync(ct);

    public async Task<Result> SetRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
    {
        // Finds a user added earlier in this use case too (CreateUser), not only stored ones.
        if (await db.Set<ApplicationUser>().FindAsync([userId], ct) is null)
            return IdentityApplicationErrors.UserNotFound;
        var wanted = roleIds.ToHashSet();
        var assignments = db.Set<IdentityUserRole<Guid>>();
        var current = await assignments.Where(x => x.UserId == userId).ToListAsync(ct);
        assignments.RemoveRange(current.Where(x => !wanted.Contains(x.RoleId)));
        foreach (var roleId in wanted.Except(current.Select(x => x.RoleId)))
            assignments.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = roleId });
        return Result.Success();
    }

    public Task<int> CountActiveUsersInRoleAsync(Guid roleId, Guid? excludingUserId, CancellationToken ct) =>
        db.Set<IdentityUserRole<Guid>>()
            .Where(x => x.RoleId == roleId && x.UserId != excludingUserId)
            .Join(db.Set<ApplicationUser>().Where(u => u.IsActive), x => x.UserId, u => u.Id, (x, u) => u.Id)
            .CountAsync(ct);

    public Task<bool> AnyUserInRoleAsync(Guid roleId, CancellationToken ct) =>
        db.Set<IdentityUserRole<Guid>>().AnyAsync(x => x.RoleId == roleId, ct);

    private Task<ApplicationUser?> FindTrackedAsync(Guid userId) => users.FindByIdAsync(userId.ToString());

    private void SpendHashTime(string password)
    {
        var hash = s_timingHash ??= users.PasswordHasher.HashPassword(new ApplicationUser(), Guid.NewGuid().ToString());
        users.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), hash, password);
    }

    private static Result Map(IdentityResult result) => result.Succeeded ? Result.Success() : IdentityResults.ToError(result, "");
}

// ASP.NET Core Identity's failures as the module's errors: a taken user name is a conflict, policy failures are
// field validation errors carrying Identity's codes (PasswordTooShort, PasswordRequiresDigit, ...).
internal static class IdentityResults
{
    public static Error ToError(IdentityResult result, string passwordProperty)
    {
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
            return IdentityApplicationErrors.UserNameTaken;
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure)))
            return CommonErrors.ConcurrencyConflict;
        return Error.Validation(result.Errors
            .Select(e => new ValidationError(PropertyFor(e.Code, passwordProperty), e.Code, e.Description))
            .ToList());
    }

    private static string PropertyFor(string code, string passwordProperty) =>
        code.StartsWith("Password", StringComparison.Ordinal) ? passwordProperty
        : code.Contains("UserName", StringComparison.Ordinal) ? "UserName"
        : code.Contains("Email", StringComparison.Ordinal) ? "Email"
        : "";
}
