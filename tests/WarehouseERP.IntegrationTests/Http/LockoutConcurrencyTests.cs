using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Identity.Infrastructure;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.IntegrationTests.Http;

// The failed-attempt count and lockout are written by atomic UPDATEs outside the use case's save, and each one renews
// the user's ConcurrencyStamp. On SQL Server: parallel guesses each count, a user row loaded before such an UPDATE
// can't be saved over it, and the request that made the UPDATE is never refused because of its own write.
[Collection(DatabaseCollection.Name)]
public sealed class LockoutConcurrencyTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const string WrongPassword = "Wrong-password-1";
    private const int Threshold = 5; // Identity:Lockout:MaxFailedAccessAttempts

    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, realSessions: true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [Theory]
    [InlineData(1)]
    [InlineData(Threshold)]
    public async Task A_user_loaded_before_failed_logins_cannot_be_saved_over_the_count_or_lockout(int failures)
    {
        var account = await _api.SeedUserAsync();
        await using var edit = _api.Services.CreateAsyncScope(); // an administrator's edit, loaded before the guesses
        var users = edit.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByIdAsync(account.Id.ToString()))!;

        for (var i = 0; i < failures; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.CreateBrowser().LoginAsync(account, WrongPassword)).StatusCode);

        user.DisplayName = "Stale edit";
        await users.UpdateAsync(user);
        var saved = await edit.ServiceProvider.GetRequiredService<IIdentityPersistence>().SaveChangesAsync(default);

        Assert.Equal(CommonErrors.ConcurrencyConflict.Code, saved.Error?.Code);
        var row = await UserRowAsync(account.Id);
        Assert.Equal("Test " + account.UserName, row.DisplayName);
        Assert.Equal(failures % Threshold, row.AccessFailedCount);
        Assert.Equal(failures >= Threshold, row.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task A_right_password_after_failed_attempts_signs_in_and_only_resets_the_count()
    {
        var account = await _api.SeedUserAsync();
        for (var i = 0; i < 2; i++)
            await _api.CreateBrowser().LoginAsync(account, WrongPassword);
        Assert.Equal(2, (await UserRowAsync(account.Id)).AccessFailedCount);

        Assert.Equal(HttpStatusCode.NoContent, (await _api.CreateBrowser().LoginAsync(account)).StatusCode);

        var row = await UserRowAsync(account.Id);
        Assert.Equal(0, row.AccessFailedCount);
        Assert.Null(row.UpdatedAtUtc); // the atomic reset only; no tracked save of the whole user row
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task A_right_password_with_an_outdated_hash_signs_in_and_saves_the_new_hash(int earlierFailures)
    {
        var account = await _api.SeedUserAsync();
        var outdated = new PasswordHasher<ApplicationUser>(Options.Create(new PasswordHasherOptions { IterationCount = 1_000 }))
            .HashPassword(new ApplicationUser(), account.Password);
        await using (var db = fx.NewDb())
            await db.Set<ApplicationUser>().Where(u => u.Id == account.Id).ExecuteUpdateAsync(s => s
                .SetProperty(u => u.PasswordHash, outdated)
                .SetProperty(u => u.ConcurrencyStamp, Guid.NewGuid().ToString()));
        for (var i = 0; i < earlierFailures; i++)
            await _api.CreateBrowser().LoginAsync(account, WrongPassword);

        // CheckPasswordAsync rehashes into the tracked user (a pending update) before the count is reset atomically.
        Assert.Equal(HttpStatusCode.NoContent, (await _api.CreateBrowser().LoginAsync(account)).StatusCode);

        var row = await UserRowAsync(account.Id);
        Assert.Equal(0, row.AccessFailedCount);
        Assert.NotEqual(outdated, row.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<ApplicationUser>().VerifyHashedPassword(new ApplicationUser(), row.PasswordHash!, account.Password));
    }

    [Theory]
    [InlineData(Threshold - 1)]
    [InlineData(Threshold)]
    public async Task Parallel_wrong_passwords_each_count_and_the_threshold_starts_one_lockout(int attempts)
    {
        var account = await _api.SeedUserAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, attempts)
            .Select(_ => _api.CreateBrowser().LoginAsync(account, WrongPassword)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
        var locks = attempts == Threshold;
        var row = await UserRowAsync(account.Id);
        Assert.Equal(locks ? 0 : attempts, row.AccessFailedCount);
        Assert.Equal(locks, row.LockoutEnd > DateTimeOffset.UtcNow);
        await using var db = fx.NewDb();
        var events = await db.Set<SecurityEvent>().Where(e => e.TargetUserId == account.Id).Select(e => e.Type).ToListAsync();
        Assert.Equal(attempts, events.Count(t => t == SecurityEventType.SignInFailed));
        Assert.Equal(locks ? 1 : 0, events.Count(t => t == SecurityEventType.UserLockedOut));
    }

    // A reset or an unlock takes several UserManager steps but saves once, against the stamp it loaded.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_reset_or_unlock_is_refused_when_the_user_changed_after_it_was_loaded(bool reset)
    {
        var account = await _api.SeedUserAsync();
        for (var i = 0; i < 2; i++) // so that an unlock takes two steps as well
            await _api.CreateBrowser().LoginAsync(account, WrongPassword);
        await using var admin = _api.Services.CreateAsyncScope();
        var credentials = admin.ServiceProvider.GetRequiredService<IUserCredentials>();
        var changed = reset
            ? await credentials.ResetPasswordAsync(account.Id, "Reset-by-admin-7", default)
            : await credentials.UnlockAsync(account.Id, default);
        Assert.True(changed.IsSuccess);

        await _api.CreateBrowser().LoginAsync(account, WrongPassword); // another request changes the row meanwhile
        var saved = await admin.ServiceProvider.GetRequiredService<IIdentityPersistence>().SaveChangesAsync(default);

        Assert.Equal(CommonErrors.ConcurrencyConflict.Code, saved.Error?.Code);
        Assert.Equal(3, (await UserRowAsync(account.Id)).AccessFailedCount); // that request's count stands
        Assert.Equal(HttpStatusCode.NoContent, (await _api.CreateBrowser().LoginAsync(account)).StatusCode); // old password
    }

    [Fact]
    public async Task A_wrong_password_is_counted_even_when_the_request_was_aborted()
    {
        var account = await _api.SeedUserAsync();
        await using var request = _api.Services.CreateAsyncScope();
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();

        var check = await request.ServiceProvider.GetRequiredService<IUserCredentials>()
            .CheckPasswordAsync(account.UserName, WrongPassword, aborted.Token);

        Assert.Equal(LoginOutcome.WrongPassword, check.Outcome);
        Assert.Equal(1, (await UserRowAsync(account.Id)).AccessFailedCount);
    }

    [Fact]
    public async Task The_request_that_counted_a_failure_can_still_save_the_user_it_loaded()
    {
        var account = await _api.SeedUserAsync();
        await using var request = _api.Services.CreateAsyncScope();
        var users = request.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByIdAsync(account.Id.ToString()))!;

        var check = await request.ServiceProvider.GetRequiredService<IUserCredentials>()
            .CheckPasswordAsync(account.UserName, WrongPassword, default);
        Assert.Equal(LoginOutcome.WrongPassword, check.Outcome);

        user.DisplayName = "Edited after the guess";
        await users.UpdateAsync(user);
        var saved = await request.ServiceProvider.GetRequiredService<IIdentityPersistence>().SaveChangesAsync(default);

        Assert.True(saved.IsSuccess, saved.Error?.Code);
        var row = await UserRowAsync(account.Id);
        Assert.Equal("Edited after the guess", row.DisplayName);
        Assert.Equal(1, row.AccessFailedCount); // the whole-row save wrote the counted value back, not the loaded 0
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_change_made_elsewhere_after_the_user_was_loaded_still_conflicts(bool rightPassword)
    {
        var account = await _api.SeedUserAsync();
        await _api.CreateBrowser().LoginAsync(account, WrongPassword); // a count for the right password to reset

        await using var request = _api.Services.CreateAsyncScope();
        var users = request.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByIdAsync(account.Id.ToString()))!;

        await using (var elsewhere = _api.Services.CreateAsyncScope())
        {
            var otherUsers = elsewhere.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var same = (await otherUsers.FindByIdAsync(account.Id.ToString()))!;
            same.DisplayName = "Edited elsewhere";
            await otherUsers.UpdateAsync(same);
            Assert.True((await elsewhere.ServiceProvider.GetRequiredService<IIdentityPersistence>().SaveChangesAsync(default)).IsSuccess);
        }

        // The password check finds the user already tracked, with the stamp from before the edit elsewhere.
        var check = await request.ServiceProvider.GetRequiredService<IUserCredentials>()
            .CheckPasswordAsync(account.UserName, rightPassword ? account.Password : WrongPassword, default);
        Assert.Equal(rightPassword ? LoginOutcome.Succeeded : LoginOutcome.WrongPassword, check.Outcome);

        user.DisplayName = "Stale edit";
        await users.UpdateAsync(user);
        var saved = await request.ServiceProvider.GetRequiredService<IIdentityPersistence>().SaveChangesAsync(default);

        Assert.Equal(CommonErrors.ConcurrencyConflict.Code, saved.Error?.Code);
        var row = await UserRowAsync(account.Id);
        Assert.Equal("Edited elsewhere", row.DisplayName);
        Assert.Equal(rightPassword ? 0 : 2, row.AccessFailedCount);
    }

    private async Task<ApplicationUser> UserRowAsync(Guid userId)
    {
        await using var db = fx.NewDb();
        return await db.Set<ApplicationUser>().AsNoTracking().SingleAsync(u => u.Id == userId);
    }
}
