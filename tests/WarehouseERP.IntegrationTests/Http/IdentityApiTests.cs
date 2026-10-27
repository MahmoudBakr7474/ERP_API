using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Identity.Infrastructure;
using WarehouseERP.Identity.Infrastructure.Persistence;
using WarehouseERP.Organization.Contracts;
using static WarehouseERP.IntegrationTests.IdentityTesting;

namespace WarehouseERP.IntegrationTests.Http;

// Identity end to end on the real host: real session authentication (cookie and bearer), real permission policies
// and scope, SQL Server underneath. Nothing here uses the test authentication scheme.
[Collection(DatabaseCollection.Name)]
public sealed class IdentityApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const string Me = "/api/identity/me";
    private const string Login = "/api/identity/login";
    private const string Logout = "/api/identity/logout";
    private const string Tokens = "/api/identity/tokens";
    private const string Users = "/api/identity/users";
    private const string Roles = "/api/identity/roles";
    private const string Warehouses = "/api/organization/warehouses";

    private static readonly string[] UnknownPermission = ["no.such.permission"];

    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, realSessions: true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    // ---- web sign-in -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Login_sets_a_hardened_session_cookie_that_authorizes_requests()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var browser = _api.CreateBrowser();

        var login = await browser.LoginAsync(account);

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var session = SetCookie(login, SessionCookie)!.ToLowerInvariant();
        foreach (var attribute in new[] { "httponly", "secure", "samesite=strict", "path=/" })
            Assert.Contains(attribute, session, StringComparison.Ordinal);
        var xsrf = SetCookie(login, XsrfCookie)!.ToLowerInvariant();
        Assert.DoesNotContain("httponly", xsrf, StringComparison.Ordinal); // the client must read it

        var me = await (await browser.GetAsync(Me)).ReadAsync<JsonElement>();
        Assert.Equal(account.Id, me.GetProperty("userId").GetGuid());
        Assert.Equal([OrganizationPermissions.WarehousesRead], me.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        Assert.True(me.GetProperty("isCompanyWide").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync(Warehouses)).StatusCode);
    }

    [Fact]
    public async Task Every_login_failure_answers_the_same_401()
    {
        var active = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var inactive = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead], active: false);
        var locked = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        for (var i = 0; i < 5; i++)
            await _api.CreateBrowser().LoginAsync(locked, "Wrong-password-1");

        var details = new List<string>();
        foreach (var (userName, password) in new[]
                 {
                     (active.UserName, "Wrong-password-1"), ("no-such-user", Password), (inactive.UserName, Password), (locked.UserName, Password),
                 })
        {
            var response = await _api.CreateBrowser().PostJsonAsync(Login, new { userName, password });
            var problem = await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.Login.Failed");
            details.Add(problem.GetProperty("detail").GetString()!);
            Assert.Null(SetCookie(response, SessionCookie));
        }
        Assert.Single(details.Distinct());
    }

    [Fact]
    public async Task Credentials_are_refused_over_plain_http()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var http = _api.CreateClient(); // http://localhost

        await (await http.PostJsonAsync(Login, new { userName = account.UserName, password = account.Password }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.Session.HttpsRequired");
        await (await http.PostJsonAsync(Tokens, new { userName = account.UserName, password = account.Password }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.Session.HttpsRequired");
    }

    [Fact]
    public async Task Lockout_refuses_even_the_right_password_until_an_administrator_unlocks()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        for (var i = 0; i < 5; i++)
            await _api.CreateBrowser().LoginAsync(account, "Wrong-password-1");

        await (await _api.CreateBrowser().LoginAsync(account)).AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.Login.Failed");

        var admin = await _api.AdministratorAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"{Users}/{account.Id}/unlock", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _api.CreateBrowser().LoginAsync(account)).StatusCode);
    }

    // A reset and an unlock each take several UserManager steps on one tracked user before the single save.
    [Fact]
    public async Task An_administrator_resets_a_password_that_had_failed_attempts_and_every_session_ends()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var before = await _api.SignedInAsync(account);
        for (var i = 0; i < 2; i++)
            await _api.CreateBrowser().LoginAsync(account, "Wrong-password-1");
        var admin = await _api.AdministratorAsync();

        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.PostJsonAsync($"{Users}/{account.Id}/password", new { newPassword = "Reset-by-admin-7" })).StatusCode);

        Assert.Equal(0, (await UserRowAsync(account.Id)).AccessFailedCount);
        await (await before.GetAsync(Me)).AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
        await (await _api.CreateBrowser().LoginAsync(account)).AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.Login.Failed");
        Assert.Equal(HttpStatusCode.NoContent, (await _api.CreateBrowser().LoginAsync(account, "Reset-by-admin-7")).StatusCode);
    }

    // The database is shared by the whole run, so this measures the difference its own users make.
    [Fact]
    public async Task Only_active_company_wide_administrators_count_as_company_wide()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        await using var scope = _api.Services.CreateAsyncScope();
        var holders = scope.ServiceProvider.GetRequiredService<IUserRoleAssignments>();
        var admin = ApplicationRole.AdministratorId;
        var anyBefore = await holders.CountActiveUsersInRoleAsync(admin, null, default);
        var companyWideBefore = await holders.CountActiveCompanyWideUsersInRoleAsync(admin, null, default);

        await _api.SeedUserAsync(administrator: true);
        await _api.SeedUserAsync(administrator: true, companyWide: false, branches: [branchId]);
        await _api.SeedUserAsync(administrator: true, active: false);

        Assert.Equal(anyBefore + 2, await holders.CountActiveUsersInRoleAsync(admin, null, default));
        Assert.Equal(companyWideBefore + 1, await holders.CountActiveCompanyWideUsersInRoleAsync(admin, null, default));
    }

    [Fact]
    public async Task An_administrator_clears_failed_attempts_of_a_user_who_is_not_locked_out()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        for (var i = 0; i < 2; i++)
            await _api.CreateBrowser().LoginAsync(account, "Wrong-password-1");
        var admin = await _api.AdministratorAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"{Users}/{account.Id}/unlock", null)).StatusCode);

        Assert.Equal(0, (await UserRowAsync(account.Id)).AccessFailedCount);
    }

    [Fact]
    public async Task Logout_ends_the_session_on_the_next_request_even_for_a_copied_cookie()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var browser = _api.CreateBrowser();
        var token = Cookie(await browser.LoginAsync(account), SessionCookie)!;

        Assert.Equal(HttpStatusCode.NoContent, (await browser.PostAsync(Logout, null)).StatusCode);

        await (await browser.GetAsync(Me)).AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
        var copy = _api.CreateBrowser(handleCookies: false);
        copy.DefaultRequestHeaders.Add("Cookie", $"{SessionCookie}={token}");
        await (await copy.GetAsync(Me)).AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
    }

    [Fact]
    public async Task Cookie_session_writes_need_the_anti_forgery_token_and_reads_do_not()
    {
        var browser = await _api.SignedInAsync(await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]));
        var token = browser.DefaultRequestHeaders.GetValues(XsrfHeader).Single();
        browser.DefaultRequestHeaders.Remove(XsrfHeader);

        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync(Me)).StatusCode);
        await (await browser.PostAsync(Logout, null)).AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.Session.CsrfTokenInvalid");
        browser.DefaultRequestHeaders.Add(XsrfHeader, "forged");
        await (await browser.PostAsync(Logout, null)).AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.Session.CsrfTokenInvalid");

        browser.DefaultRequestHeaders.Remove(XsrfHeader);
        browser.DefaultRequestHeaders.Add(XsrfHeader, token);
        Assert.Equal(HttpStatusCode.NoContent, (await browser.PostAsync(Logout, null)).StatusCode);
    }

    [Fact]
    public async Task Idle_web_session_ends_after_the_idle_timeout()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await using var api = new ApiFactory(fx.ConnectionString, clock: clock, realSessions: true);
        var browser = await api.SignedInAsync(await api.SeedUserAsync([OrganizationPermissions.WarehousesRead]));
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync(Me)).StatusCode);

        clock.Advance(TimeSpan.FromHours(8) + TimeSpan.FromMinutes(1)); // Identity:Sessions:WebIdleTimeout

        await (await browser.GetAsync(Me)).AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
    }

    // ---- revocation --------------------------------------------------------------------------------------------

    [Fact]
    public async Task Administrator_revocation_applies_on_the_next_request()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var user = await _api.SignedInAsync(account);
        var admin = await _api.AdministratorAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"{Users}/{account.Id}/sessions/revoke", null)).StatusCode);

        await (await user.GetAsync(Me)).AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
    }

    [Fact]
    public async Task Deactivation_signs_the_user_out_and_blocks_sign_in()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var user = await _api.SignedInAsync(account);
        var admin = await _api.AdministratorAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"{Users}/{account.Id}/deactivate", null)).StatusCode);

        await (await user.GetAsync(Me)).AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
        await (await _api.CreateBrowser().LoginAsync(account)).AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.Login.Failed");
    }

    [Fact]
    public async Task Users_end_their_own_sessions_but_cannot_see_anyone_elses()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var browser = await _api.SignedInAsync(account);
        var pos = await IssueTokensAsync(account);
        var stranger = await IssueTokensAsync(await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]));

        var mine = await (await browser.GetAsync("/api/identity/sessions")).ReadAsync<JsonElement>();
        Assert.Contains(mine.EnumerateArray(), s => s.GetProperty("id").GetGuid() == pos.SessionId);

        await (await browser.PostAsync($"/api/identity/sessions/{stranger.SessionId}/revoke", null))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Identity.Session.NotFound");
        Assert.Equal(HttpStatusCode.NoContent, (await browser.PostAsync($"/api/identity/sessions/{pos.SessionId}/revoke", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Bare().SendAsBearerAsync(HttpMethod.Get, Me, pos.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Bare().SendAsBearerAsync(HttpMethod.Get, Me, stranger.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Changing_the_password_keeps_this_session_and_ends_the_others()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var here = await _api.SignedInAsync(account);
        var elsewhere = await _api.SignedInAsync(account);

        var changed = await here.PostJsonAsync("/api/identity/me/password", new { currentPassword = account.Password, newPassword = "New-Password-42" });

        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await here.GetAsync(Me)).StatusCode);
        await (await elsewhere.GetAsync(Me)).AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
        await (await _api.CreateBrowser().LoginAsync(account)).AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.Login.Failed");
        Assert.Equal(HttpStatusCode.NoContent, (await _api.CreateBrowser().LoginAsync(account, "New-Password-42")).StatusCode);
    }

    [Fact]
    public async Task Wrong_current_password_does_not_change_it()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var browser = await _api.SignedInAsync(account);

        var response = await browser.PostJsonAsync("/api/identity/me/password", new { currentPassword = "Not-my-password-1", newPassword = "New-Password-42" });

        await response.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Identity.Password.Incorrect");
        Assert.Equal(HttpStatusCode.NoContent, (await _api.CreateBrowser().LoginAsync(account)).StatusCode);
    }

    // ---- authorization -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Removing_a_permission_applies_on_the_next_request_without_signing_in_again()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var user = await _api.SignedInAsync(account);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync(Warehouses)).StatusCode);

        var admin = await _api.AdministratorAsync();
        var version = await RoleVersionAsync(admin, account.RoleId!.Value);
        var updated = await admin.PutJsonAsync($"{Roles}/{account.RoleId}", new { name = DatabaseFixture.Code("role"), permissions = Array.Empty<string>(), version });
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);

        await (await user.GetAsync(Warehouses)).AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
    }

    [Fact]
    public async Task Branch_grant_covers_a_warehouse_created_later_and_nothing_outside_it()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var (otherBranchId, _) = await fx.SeedBranchAndUnitAsync();
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead], companyWide: false, branches: [branchId]);
        var user = await _api.SignedInAsync(account);
        var admin = await _api.AdministratorAsync();

        var later = await CreateWarehouseAsync(admin, branchId);
        var outside = await CreateWarehouseAsync(admin, otherBranchId);

        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync($"{Warehouses}/{later}")).StatusCode);
        await (await user.GetAsync($"{Warehouses}/{outside}")).AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Warehouse.AccessDenied");
        var me = await (await user.GetAsync(Me)).ReadAsync<JsonElement>();
        var scope = me.GetProperty("warehouseIds").EnumerateArray().Select(w => w.GetGuid()).ToList();
        Assert.Contains(later, scope);
        Assert.DoesNotContain(outside, scope);
    }

    [Fact]
    public async Task Scope_is_granted_per_warehouse_and_unknown_ids_are_refused()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var admin = await _api.AdministratorAsync();
        var granted = await CreateWarehouseAsync(admin, branchId);
        var other = await CreateWarehouseAsync(admin, branchId);
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead], companyWide: false);
        var access = $"{Users}/{account.Id}/access";

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutJsonAsync(access, new { isCompanyWide = false, warehouseIds = new[] { granted }, revision = 0 })).StatusCode);
        await (await admin.PutJsonAsync(access, new { warehouseIds = new[] { Guid.NewGuid() }, revision = 1 }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Identity.Access.UnknownWarehouse");
        await (await admin.PutJsonAsync(access, new { branchIds = new[] { Guid.NewGuid() }, revision = 1 }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Identity.Access.UnknownBranch");

        var user = await _api.SignedInAsync(account);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync($"{Warehouses}/{granted}")).StatusCode);
        await (await user.GetAsync($"{Warehouses}/{other}")).AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Warehouse.AccessDenied");
        var details = await (await admin.GetAsync($"{Users}/{account.Id}")).ReadAsync<JsonElement>();
        Assert.Equal(granted, Assert.Single(details.GetProperty("access").GetProperty("warehouseIds").EnumerateArray()).GetGuid());
    }

    [Fact]
    public async Task A_scoped_manager_grants_only_within_their_own_scope()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var (otherBranchId, _) = await fx.SeedBranchAndUnitAsync();
        var admin = await _api.AdministratorAsync();
        var inside = await CreateWarehouseAsync(admin, branchId);
        var outside = await CreateWarehouseAsync(admin, otherBranchId);
        var manager = await _api.SeedUserAsync(
            [IdentityPermissions.AccessManage, OrganizationPermissions.WarehousesRead], companyWide: false, branches: [branchId]);
        var target = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead], companyWide: false);
        var client = await _api.SignedInAsync(manager);
        var access = $"{Users}/{target.Id}/access";

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutJsonAsync(access, new { warehouseIds = new[] { inside }, revision = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutJsonAsync(access, new { branchIds = new[] { branchId }, revision = 1 })).StatusCode);
        await (await client.PutJsonAsync(access, new { warehouseIds = new[] { outside }, revision = 2 }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.Access.ExceedsOwnScope");
        await (await client.PutJsonAsync(access, new { branchIds = new[] { otherBranchId }, revision = 2 }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.Access.ExceedsOwnScope");
    }

    [Fact]
    public async Task A_user_manager_cannot_grant_or_take_over_more_than_they_hold()
    {
        var manager = await _api.SeedUserAsync(
            [IdentityPermissions.UsersRead, IdentityPermissions.UsersManage, IdentityPermissions.AccessManage], companyWide: false);
        var administrator = await _api.SeedUserAsync(administrator: true);
        var client = await _api.SignedInAsync(manager);

        await (await client.PutJsonAsync($"{Users}/{manager.Id}/roles", new { roleIds = new[] { ApplicationRole.AdministratorId } }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.Access.ExceedsOwnPermissions");
        await (await client.PostJsonAsync($"{Users}/{administrator.Id}/password", new { newPassword = "Taken-over-123" }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.Access.ExceedsOwnPermissions");
        await (await client.PutJsonAsync($"{Users}/{manager.Id}/access", new { isCompanyWide = true }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.Access.ExceedsOwnScope");
    }

    // ---- POS / API tokens --------------------------------------------------------------------------------------

    [Fact]
    public async Task Api_tokens_rotate_and_a_reused_refresh_token_ends_the_session()
    {
        var first = await IssueTokensAsync(await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]));
        Assert.Equal(HttpStatusCode.OK, (await Bare().SendAsBearerAsync(HttpMethod.Get, Me, first.AccessToken)).StatusCode);

        var second = await RefreshAsync(first.RefreshToken);
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Bare().SendAsBearerAsync(HttpMethod.Get, Me, first.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Bare().SendAsBearerAsync(HttpMethod.Get, Me, second.AccessToken)).StatusCode);

        // The first refresh token was used already: someone holds a copy, so the whole session ends.
        await (await Bare().PostJsonAsync($"{Tokens}/refresh", new { refreshToken = first.RefreshToken }))
            .AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.Session.RefreshTokenReused");
        Assert.Equal(HttpStatusCode.Unauthorized, (await Bare().SendAsBearerAsync(HttpMethod.Get, Me, second.AccessToken)).StatusCode);
        await (await Bare().PostJsonAsync($"{Tokens}/refresh", new { refreshToken = second.RefreshToken }))
            .AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.Session.Revoked");
    }

    // Whichever request commits first, the outcome is the same: one rotation, then the other request ends the session.
    [Fact]
    public async Task Two_refreshes_racing_with_one_token_end_the_session()
    {
        var issued = await IssueTokensAsync(await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]));

        var responses = await Task.WhenAll(Enumerable.Range(0, 2)
            .Select(_ => Bare().PostJsonAsync($"{Tokens}/refresh", new { refreshToken = issued.RefreshToken })));

        var winner = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        await Assert.Single(responses, r => r != winner)
            .AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.Session.RefreshTokenReused");
        var rotated = Read(await winner.ReadAsync<JsonElement>());
        Assert.Equal(HttpStatusCode.Unauthorized, (await Bare().SendAsBearerAsync(HttpMethod.Get, Me, rotated.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task A_refresh_token_from_two_rotations_ago_ends_the_session()
    {
        var first = await IssueTokensAsync(await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]));
        var third = await RefreshAsync((await RefreshAsync(first.RefreshToken)).RefreshToken);

        await (await Bare().PostJsonAsync($"{Tokens}/refresh", new { refreshToken = first.RefreshToken }))
            .AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.Session.RefreshTokenReused");

        Assert.Equal(HttpStatusCode.Unauthorized, (await Bare().SendAsBearerAsync(HttpMethod.Get, Me, third.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Session_cleanup_deletes_only_sessions_that_ended_before_the_retention()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var now = DateTime.UtcNow;
        var longAgo = now.AddDays(-60); // Identity:Sessions:RetentionAfterEnd is 30 days
        var expired = UserSession.StartWeb(account.Id, SessionTokens.Create().Hash, now.AddDays(-32), TimeSpan.FromDays(1)); // ended 31 days ago
        var revokedWithinRetention = UserSession.StartWeb(account.Id, SessionTokens.Create().Hash, now.AddDays(-30), TimeSpan.FromDays(7));
        revokedWithinRetention.Revoke(SessionRevokeReason.Logout, now.AddDays(-29)); // ended 29 days ago: kept
        var live = UserSession.StartWeb(account.Id, SessionTokens.Create().Hash, now, TimeSpan.FromHours(12));
        var refreshHash = SessionTokens.Create().Hash;
        var rotated = UserSession.StartApi(account.Id, "POS-01",
            new IssuedApiTokens(SessionTokens.Create().Hash, longAgo.AddMinutes(15), refreshHash, longAgo.AddDays(10)), longAgo, TimeSpan.FromDays(20));
        Assert.True(rotated.Refresh(refreshHash,
            new IssuedApiTokens(SessionTokens.Create().Hash, longAgo.AddMinutes(30), SessionTokens.Create().Hash, longAgo.AddDays(10)), longAgo.AddMinutes(10)).IsSuccess);
        await using (var db = fx.NewDb())
        {
            db.AddRange(expired, revokedWithinRetention, live, rotated);
            await db.SaveChangesAsync();
        }

        await _api.Services.GetRequiredService<ExpiredSessionCleanup>().CleanUpAsync(default);

        await using var check = fx.NewDb();
        var left = await check.Set<UserSession>().Where(s => s.UserId == account.Id).Select(s => s.Id).ToListAsync();
        Assert.Equal(new[] { live.Id, revokedWithinRetention.Id }.Order(), left.Order());
        Assert.False(await check.Set<RetiredRefreshToken>().AnyAsync(r => r.SessionId == rotated.Id)); // gone with its session
    }

    [Fact]
    public async Task A_stale_access_revision_is_refused()
    {
        var admin = await _api.AdministratorAsync();
        var target = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var revision = await AccessRevisionAsync(admin, target.Id);
        var access = $"{Users}/{target.Id}/access";

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutJsonAsync(access, new { isCompanyWide = true, revision })).StatusCode);

        await (await admin.PutJsonAsync(access, new { isCompanyWide = true, revision }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");
    }

    [Fact]
    public async Task Guessing_the_current_password_locks_the_account_and_records_the_lockout()
    {
        var account = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        var browser = await _api.SignedInAsync(account);
        var guess = new { currentPassword = "Wrong-password-1", newPassword = "New-Password-42" };

        for (var i = 0; i < 5; i++) // Identity:Lockout:MaxFailedAccessAttempts
            await (await browser.PostJsonAsync("/api/identity/me/password", guess))
                .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Identity.Password.Incorrect");

        await (await browser.PostJsonAsync("/api/identity/me/password", new { currentPassword = account.Password, newPassword = "New-Password-42" }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Identity.Password.LockedOut");
        await using var db = fx.NewDb();
        Assert.Single(await db.Set<SecurityEvent>()
            .Where(e => e.TargetUserId == account.Id && e.Type == SecurityEventType.UserLockedOut).ToListAsync());
    }

    [Fact]
    public async Task The_audit_trail_needs_identity_audit_read()
    {
        var reader = await _api.SignedInAsync(await _api.SeedUserAsync([IdentityPermissions.UsersRead]));
        var auditor = await _api.SignedInAsync(await _api.SeedUserAsync([IdentityPermissions.AuditRead]));

        await (await reader.GetAsync("/api/identity/security-events")).AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
        var page = await auditor.GetAsync("/api/identity/security-events?type=SignedIn");

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.All((await page.ReadAsync<JsonElement>()).GetProperty("items").EnumerateArray(),
            e => Assert.Equal("SignedIn", e.GetProperty("type").GetString()));
    }

    [Fact]
    public async Task The_password_policy_is_shown_to_signed_in_users()
    {
        await (await _api.CreateBrowser().GetAsync("/api/identity/password-policy"))
            .AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
        var browser = await _api.SignedInAsync(await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]));

        var policy = await (await browser.GetAsync("/api/identity/password-policy")).ReadAsync<JsonElement>();

        Assert.Equal(10, policy.GetProperty("requiredLength").GetInt32());
        Assert.True(policy.GetProperty("requireDigit").GetBoolean());
    }

    [Fact]
    public async Task Bearer_session_needs_no_anti_forgery_token_and_can_be_revoked()
    {
        var tokens = await IssueTokensAsync(await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]));

        Assert.Equal(HttpStatusCode.NoContent, (await Bare().SendAsBearerAsync(HttpMethod.Post, $"{Tokens}/revoke", tokens.AccessToken)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Bare().SendAsBearerAsync(HttpMethod.Get, Me, tokens.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task A_web_session_token_is_not_accepted_as_a_bearer_token()
    {
        var token = Cookie(await _api.CreateBrowser().LoginAsync(await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead])), SessionCookie)!;

        Assert.Equal(HttpStatusCode.Unauthorized, (await Bare().SendAsBearerAsync(HttpMethod.Get, Me, token)).StatusCode);
    }

    // ---- administration ----------------------------------------------------------------------------------------

    [Fact]
    public async Task Administrator_creates_a_role_and_a_scoped_user_who_can_then_sign_in()
    {
        var admin = await _api.AdministratorAsync();
        var catalog = await (await admin.GetAsync("/api/identity/permissions")).ReadAsync<JsonElement>();
        var adminMe = await (await admin.GetAsync(Me)).ReadAsync<JsonElement>();
        Assert.Equal(catalog.GetArrayLength(), adminMe.GetProperty("permissions").GetArrayLength()); // every permission

        var roleId = await CreatedIdAsync(await admin.PostJsonAsync(Roles,
            new { name = DatabaseFixture.Code("role"), permissions = new[] { OrganizationPermissions.WarehousesRead } }));
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var userName = DatabaseFixture.Code("new").ToLowerInvariant();
        var userId = await CreatedIdAsync(await admin.PostJsonAsync(Users, new
        {
            userName,
            displayName = "مستخدم جديد",
            password = Password,
            roleIds = new[] { roleId },
            isCompanyWide = false,
            branchIds = new[] { branchId },
        }));

        var details = await (await admin.GetAsync($"{Users}/{userId}")).ReadAsync<JsonElement>();
        Assert.Equal(roleId, Assert.Single(details.GetProperty("roles").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(branchId, Assert.Single(details.GetProperty("access").GetProperty("branchIds").EnumerateArray()).GetGuid());
        var found = await (await admin.GetAsync($"{Users}?search={userName}")).ReadAsync<JsonElement>();
        Assert.Equal(1, found.GetProperty("totalCount").GetInt32());

        var user = await _api.SignedInAsync(new TestAccount(userId, userName, Password, roleId));
        var me = await (await user.GetAsync(Me)).ReadAsync<JsonElement>();
        Assert.Equal([OrganizationPermissions.WarehousesRead], me.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        Assert.False(me.GetProperty("isCompanyWide").GetBoolean());
    }

    [Fact]
    public async Task User_creation_reports_taken_names_and_weak_passwords()
    {
        var admin = await _api.AdministratorAsync();
        var existing = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);

        await (await admin.PostJsonAsync(Users, new { userName = existing.UserName, displayName = "x", password = Password }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Identity.User.UserNameTaken");
        var weak = await (await admin.PostJsonAsync(Users, new { userName = DatabaseFixture.Code("w").ToLowerInvariant(), displayName = "x", password = "short" }))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        Assert.Contains(weak.GetProperty("errors").EnumerateArray(),
            e => e.GetProperty("property").GetString() == "password" && e.GetProperty("code").GetString() == "PasswordTooShort");
    }

    [Fact]
    public async Task Role_and_user_rules_answer_with_their_error_codes()
    {
        var adminAccount = await _api.SeedUserAsync(administrator: true);
        var admin = await _api.SignedInAsync(adminAccount);
        var holder = await _api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);

        await (await admin.PutJsonAsync($"{Roles}/{ApplicationRole.AdministratorId}",
                new { name = "Administrator", permissions = Array.Empty<string>(), version = await RoleVersionAsync(admin, ApplicationRole.AdministratorId) }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Identity.Role.SystemRoleLocked");
        await (await admin.PostJsonAsync(Roles, new { name = DatabaseFixture.Code("role"), permissions = UnknownPermission }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Identity.Role.UnknownPermission");
        await (await admin.PostJsonAsync(Roles, new { name = "administrator", permissions = Array.Empty<string>() }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Identity.Role.NameTaken");
        await (await admin.DeleteAsync($"{Roles}/{holder.RoleId}"))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Identity.Role.InUse");
        await (await admin.PostAsync($"{Users}/{adminAccount.Id}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Identity.User.CannotDeactivateSelf");
    }

    [Fact]
    public async Task Identity_endpoints_require_their_permissions()
    {
        var reader = await _api.SignedInAsync(await _api.SeedUserAsync([IdentityPermissions.UsersRead]));

        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync(Users)).StatusCode);
        await (await reader.GetAsync(Roles)).AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
        await (await reader.PostJsonAsync(Users, new { userName = "x1", displayName = "x", password = Password }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
        await (await _api.CreateBrowser().GetAsync(Users)).AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
    }

    // ---- logging -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Sign_in_never_logs_passwords_tokens_or_typed_user_names()
    {
        var sink = new CollectingSink();
        await using var api = new ApiFactory(fx.ConnectionString, logSink: sink, realSessions: true,
            settings: new Dictionary<string, string?> { ["Serilog:MinimumLevel:Default"] = "Verbose" });
        var account = await api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);
        const string mistypedSecret = "Typed-in-user-field-7";

        await api.CreateBrowser().PostJsonAsync(Login, new { userName = mistypedSecret, password = "Wrong-password-1" });
        var token = Cookie(await api.CreateBrowser().LoginAsync(account), SessionCookie)!;
        var tokens = await (await api.CreateBrowser().PostJsonAsync(Tokens, new { userName = account.UserName, password = account.Password }))
            .ReadAsync<JsonElement>();

        var logged = string.Join('\n', sink.Events.Select(e => e.RenderMessage(System.Globalization.CultureInfo.InvariantCulture) + " " + string.Join(' ', e.Properties.Values)));
        Assert.Contains("signed in", logged, StringComparison.Ordinal); // the events are there...
        foreach (var secret in new[]
                 {
                     account.Password, "Wrong-password-1", mistypedSecret, token,
                     tokens.GetProperty("accessToken").GetString()!, tokens.GetProperty("refreshToken").GetString()!,
                 })
            Assert.DoesNotContain(secret, logged, StringComparison.Ordinal); // ...without any secret
    }

    // ---- helpers -----------------------------------------------------------------------------------------------

    private sealed record Issued(Guid SessionId, string AccessToken, string RefreshToken);

    private HttpClient Bare() => _api.CreateBrowser(handleCookies: false);

    private async Task<Issued> IssueTokensAsync(TestAccount account)
    {
        var response = await Bare().PostJsonAsync(Tokens, new { userName = account.UserName, password = account.Password, deviceId = "POS-01" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Read(await response.ReadAsync<JsonElement>());
    }

    private async Task<Issued> RefreshAsync(string refreshToken)
    {
        var response = await Bare().PostJsonAsync($"{Tokens}/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Read(await response.ReadAsync<JsonElement>());
    }

    private static Issued Read(JsonElement json) =>
        new(json.GetProperty("sessionId").GetGuid(), json.GetProperty("accessToken").GetString()!, json.GetProperty("refreshToken").GetString()!);

    private static async Task<Guid> CreateWarehouseAsync(HttpClient admin, Guid branchId) =>
        await CreatedIdAsync(await admin.PostJsonAsync(Warehouses,
            new { branchId, code = DatabaseFixture.Code("WH"), nameAr = "مخزن", type = WarehouseType.Physical, allowsSelling = true }));

    private static async Task<Guid> CreatedIdAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"Expected 201, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
    }

    private async Task<ApplicationUser> UserRowAsync(Guid userId)
    {
        await using var db = fx.NewDb();
        return await db.Set<ApplicationUser>().AsNoTracking().SingleAsync(u => u.Id == userId);
    }
}

// The first administrator needs an empty user table, so this runs on a database of its own.
public sealed class IdentityBootstrapTests : IAsyncLifetime
{
    private readonly DatabaseFixture _database = new();

    public Task InitializeAsync() => _database.InitializeAsync();

    public Task DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task First_administrator_comes_from_settings_only_while_no_user_exists()
    {
        await using (var api = new ApiFactory(_database.ConnectionString, realSessions: true, settings: Bootstrap("root", "Bootstrap-Pass-1")))
        {
            var browser = api.CreateBrowser();
            Assert.Equal(HttpStatusCode.NoContent,
                (await browser.PostJsonAsync("/api/identity/login", new { userName = "root", password = "Bootstrap-Pass-1" })).StatusCode);
            var me = await (await browser.GetAsync("/api/identity/me")).ReadAsync<JsonElement>();
            Assert.Equal(["Administrator"], me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
            Assert.True(me.GetProperty("isCompanyWide").GetBoolean());
            Assert.Contains(IdentityPermissions.UsersManage, me.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        }

        await using (var api = new ApiFactory(_database.ConnectionString, realSessions: true, settings: Bootstrap("second", "Another-Pass-1")))
        {
            var response = await api.CreateBrowser().PostJsonAsync("/api/identity/login", new { userName = "second", password = "Another-Pass-1" });
            await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "Identity.Login.Failed");
        }
    }

    private static Dictionary<string, string?> Bootstrap(string userName, string password) => new()
    {
        ["Identity:Bootstrap:UserName"] = userName,
        ["Identity:Bootstrap:Password"] = password,
    };
}
