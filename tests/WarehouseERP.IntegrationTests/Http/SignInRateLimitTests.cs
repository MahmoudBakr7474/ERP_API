using System.Net;

namespace WarehouseERP.IntegrationTests.Http;

// Each credential endpoint has its own fixed window per client IP (Identity:SignInRateLimit:<operation id>), so one
// busy endpoint never spends another's budget: a branch's POS refreshes, all from the branch's one public IP, can't
// keep its users from signing in.
[Collection(DatabaseCollection.Name)]
public sealed class SignInRateLimitTests(DatabaseFixture fx)
{
    private const string WrongPassword = "Wrong-password-1";

    [Fact]
    public async Task A_spent_login_budget_refuses_logins_only()
    {
        await using var api = Api(("Identity:SignInRateLimit:Login:PermitLimit", "2"));
        var account = await api.SeedUserAsync();
        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateBrowser().LoginAsync(account, WrongPassword)).StatusCode);

        var refused = await api.CreateBrowser().LoginAsync(account);

        await refused.AssertProblemAsync(HttpStatusCode.TooManyRequests, "Common.TooManyRequests");
        Assert.True(refused.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        var tokens = await api.CreateBrowser().PostJsonAsync("/api/identity/tokens", new { userName = account.UserName, password = account.Password });
        Assert.Equal(HttpStatusCode.OK, tokens.StatusCode); // same client IP, its own budget
    }

    [Fact]
    public async Task A_spent_refresh_budget_refuses_refreshes_only()
    {
        await using var api = Api(("Identity:SignInRateLimit:RefreshApiToken:PermitLimit", "1"));
        var account = await api.SeedUserAsync();
        var refresh = new { refreshToken = "not-a-token" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateBrowser().PostJsonAsync("/api/identity/tokens/refresh", refresh)).StatusCode);

        await (await api.CreateBrowser().PostJsonAsync("/api/identity/tokens/refresh", refresh))
            .AssertProblemAsync(HttpStatusCode.TooManyRequests, "Common.TooManyRequests");

        Assert.Equal(HttpStatusCode.NoContent, (await api.CreateBrowser().LoginAsync(account)).StatusCode);
    }

    [Fact]
    public async Task A_spent_password_change_budget_refuses_password_changes_only()
    {
        await using var api = Api(("Identity:SignInRateLimit:ChangeMyPassword:PermitLimit", "1"));
        var account = await api.SeedUserAsync();
        var browser = await api.SignedInAsync(account);
        var change = new { currentPassword = WrongPassword, newPassword = "New-Password-42" };
        await (await browser.PostJsonAsync("/api/identity/me/password", change))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Identity.Password.Incorrect");

        await (await browser.PostJsonAsync("/api/identity/me/password", change))
            .AssertProblemAsync(HttpStatusCode.TooManyRequests, "Common.TooManyRequests");

        Assert.Equal(HttpStatusCode.NoContent, (await api.CreateBrowser().LoginAsync(account)).StatusCode);
    }

    private ApiFactory Api(params (string Key, string Value)[] settings) =>
        new(fx.ConnectionString, realSessions: true, settings: settings.ToDictionary(s => s.Key, s => (string?)s.Value));
}
