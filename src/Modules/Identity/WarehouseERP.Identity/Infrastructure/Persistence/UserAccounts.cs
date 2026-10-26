using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

// IUserAccounts over ASP.NET Core Identity's UserManager. The store runs with AutoSaveChanges off, so nothing here
// writes: the use case's IIdentityPersistence.SaveChangesAsync commits everything at once.
internal sealed class UserAccounts(UserManager<ApplicationUser> users, DbContext db, TimeProvider clock) : IUserAccounts
{
    public Task<UserAccount?> FindAsync(Guid userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        return db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserAccount(u.Id, u.UserName!, u.DisplayName, u.Email, u.IsActive, u.LockoutEnd != null && u.LockoutEnd > now))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<Result<Guid>> CreateAsync(NewUserAccount account, CancellationToken ct)
    {
        var user = new ApplicationUser { UserName = account.UserName, Email = account.Email, DisplayName = account.DisplayName };
        var created = await users.CreateAsync(user, account.Password);
        return created.Succeeded ? user.Id : IdentityResults.ToError(created, nameof(NewUserAccount.Password));
    }

    public async Task<Result> UpdateProfileAsync(Guid userId, string displayName, string? email, CancellationToken ct)
    {
        if (await users.FindByIdAsync(userId.ToString()) is not { } user)
            return UserErrors.NotFound;
        user.DisplayName = displayName;
        user.Email = email;
        return IdentityResults.ToResult(await users.UpdateAsync(user));
    }

    public async Task<Result> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct)
    {
        if (await users.FindByIdAsync(userId.ToString()) is not { } user)
            return UserErrors.NotFound;
        user.IsActive = isActive;
        return IdentityResults.ToResult(isActive ? await users.UpdateAsync(user) : await users.UpdateSecurityStampAsync(user));
    }
}
