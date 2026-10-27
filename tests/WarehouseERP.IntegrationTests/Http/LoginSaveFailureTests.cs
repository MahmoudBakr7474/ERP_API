using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Identity.Infrastructure;
using WarehouseERP.Persistence;

namespace WarehouseERP.IntegrationTests.Http;

// What a login leaves behind when its final save fails. No transaction spans a login: the failed-attempt count, a
// lockout and a right password's reset are committed first by their own atomic UPDATEs; the security events, the
// session and a rehashed password follow in the use case's single save. So a failed save (injected here for one
// user's security events, as a database error would) never undoes a counted guess and never keeps half a sign-in.
[Collection(DatabaseCollection.Name)]
public sealed class LoginSaveFailureTests(DatabaseFixture fx) : IAsyncLifetime
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
    public async Task A_counted_failure_stays_counted_when_its_security_events_cannot_be_saved(int failures)
    {
        var account = await _api.SeedUserAsync();

        await using (var failing = FailingSecurityEventSaves(account.Id))
            for (var i = 0; i < failures; i++)
                await (await Browser(failing).LoginAsync(account, WrongPassword))
                    .AssertProblemAsync(HttpStatusCode.InternalServerError, "Common.Unexpected");

        var row = await UserRowAsync(account.Id);
        Assert.Equal(failures % Threshold, row.AccessFailedCount);
        Assert.Equal(failures >= Threshold, row.LockoutEnd > DateTimeOffset.UtcNow);
        Assert.Empty(await EventsAsync(account.Id)); // the audit gap the design accepts
    }

    [Fact]
    public async Task A_right_password_resets_the_count_even_when_the_session_cannot_be_saved()
    {
        var account = await _api.SeedUserAsync();
        for (var i = 0; i < 2; i++)
            await _api.CreateBrowser().LoginAsync(account, WrongPassword);

        await using (var failing = FailingSecurityEventSaves(account.Id))
            await (await Browser(failing).LoginAsync(account))
                .AssertProblemAsync(HttpStatusCode.InternalServerError, "Common.Unexpected");

        Assert.Equal(0, (await UserRowAsync(account.Id)).AccessFailedCount);
        await using (var db = fx.NewDb())
            Assert.False(await db.Set<UserSession>().AnyAsync(s => s.UserId == account.Id)); // no session without its event
        Assert.DoesNotContain(SecurityEventType.SignedIn, await EventsAsync(account.Id));
        Assert.Equal(HttpStatusCode.NoContent, (await _api.CreateBrowser().LoginAsync(account)).StatusCode);
    }

    [Fact]
    public async Task A_rehashed_password_is_not_kept_when_the_login_cannot_be_saved()
    {
        var account = await _api.SeedUserAsync();
        var outdated = new PasswordHasher<ApplicationUser>(Options.Create(new PasswordHasherOptions { IterationCount = 1_000 }))
            .HashPassword(new ApplicationUser(), account.Password);
        await using (var db = fx.NewDb())
            await db.Set<ApplicationUser>().Where(u => u.Id == account.Id).ExecuteUpdateAsync(s => s
                .SetProperty(u => u.PasswordHash, outdated)
                .SetProperty(u => u.ConcurrencyStamp, Guid.NewGuid().ToString()));
        for (var i = 0; i < 2; i++)
            await _api.CreateBrowser().LoginAsync(account, WrongPassword);

        await using (var failing = FailingSecurityEventSaves(account.Id))
            await (await Browser(failing).LoginAsync(account))
                .AssertProblemAsync(HttpStatusCode.InternalServerError, "Common.Unexpected");

        var row = await UserRowAsync(account.Id);
        Assert.Equal(outdated, row.PasswordHash); // rolled back with the session; it still verifies the same password
        Assert.Equal(0, row.AccessFailedCount); // the reset was committed on its own
        Assert.Equal(HttpStatusCode.NoContent, (await _api.CreateBrowser().LoginAsync(account)).StatusCode);
        Assert.NotEqual(outdated, (await UserRowAsync(account.Id)).PasswordHash); // the next sign-in rehashes
    }

    [Fact]
    public async Task An_inactive_users_refused_login_saves_only_its_security_event()
    {
        var account = await _api.SeedUserAsync(active: false);
        var outdated = new PasswordHasher<ApplicationUser>(Options.Create(new PasswordHasherOptions { IterationCount = 1_000 }))
            .HashPassword(new ApplicationUser(), account.Password);
        await using (var db = fx.NewDb())
            await db.Set<ApplicationUser>().Where(u => u.Id == account.Id).ExecuteUpdateAsync(s => s
                .SetProperty(u => u.PasswordHash, outdated)
                .SetProperty(u => u.ConcurrencyStamp, Guid.NewGuid().ToString()));

        // The right password: verified, and rehashed into the tracked user, before the account is found inactive.
        await (await _api.CreateBrowser().LoginAsync(account))
            .AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.Login.Failed");

        var row = await UserRowAsync(account.Id);
        Assert.Equal(outdated, row.PasswordHash);
        Assert.Null(row.UpdatedAtUtc);
        Assert.Equal([SecurityEventType.SignInFailed], await EventsAsync(account.Id));
    }

    // The same host, except that saving a security event about this user fails.
    private WebApplicationFactory<Program> FailingSecurityEventSaves(Guid userId) =>
        _api.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.ConfigureDbContext<ErpDbContext>(o => o.AddInterceptors(new FailSecurityEventSaves(userId)))));

    private static HttpClient Browser(WebApplicationFactory<Program> api) =>
        api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    private async Task<ApplicationUser> UserRowAsync(Guid userId)
    {
        await using var db = fx.NewDb();
        return await db.Set<ApplicationUser>().AsNoTracking().SingleAsync(u => u.Id == userId);
    }

    private async Task<List<SecurityEventType>> EventsAsync(Guid userId)
    {
        await using var db = fx.NewDb();
        return await db.Set<SecurityEvent>().Where(e => e.TargetUserId == userId).Select(e => e.Type).ToListAsync();
    }

    private sealed class FailSecurityEventSaves(Guid userId) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            eventData.Context!.ChangeTracker.Entries<SecurityEvent>()
                .Any(e => e.State == EntityState.Added && e.Entity.TargetUserId == userId)
                ? throw new DbUpdateException("Injected failure: security events could not be saved.")
                : ValueTask.FromResult(result);
    }
}
