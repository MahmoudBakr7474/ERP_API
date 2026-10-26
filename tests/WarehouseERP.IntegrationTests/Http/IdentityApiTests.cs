using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.Identity.Infrastructure;
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
        var updated = await admin.PutJsonAsync($"{Roles}/{account.RoleId}", new { name = DatabaseFixture.Code("role"), permissions = Array.Empty<string>() });
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

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutJsonAsync(access, new { isCompanyWide = false, warehouseIds = new[] { granted } })).StatusCode);
        await (await admin.PutJsonAsync(access, new { warehouseIds = new[] { Guid.NewGuid() } }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Identity.Access.UnknownWarehouse");
        await (await admin.PutJsonAsync(access, new { branchIds = new[] { Guid.NewGuid() } }))
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

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutJsonAsync(access, new { warehouseIds = new[] { inside } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutJsonAsync(access, new { branchIds = new[] { branchId } })).StatusCode);
        await (await client.PutJsonAsync(access, new { warehouseIds = new[] { outside } }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.Access.ExceedsOwnScope");
        await (await client.PutJsonAsync(access, new { branchIds = new[] { otherBranchId } }))
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
            userName, displayName = "مستخدم جديد", password = Password, roleIds = new[] { roleId }, isCompanyWide = false, branchIds = new[] { branchId },
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

        await (await admin.PutJsonAsync($"{Roles}/{ApplicationRole.AdministratorId}", new { name = "Administrator", permissions = Array.Empty<string>() }))
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
