using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Identity.Infrastructure;

namespace WarehouseERP.IntegrationTests;

/// <param name="RoleId">The role made for the user's permissions (or the Administrator role), if any.</param>
internal sealed record TestAccount(Guid Id, string UserName, string Password, Guid? RoleId);

// Users are seeded straight through ASP.NET Core Identity, as the bootstrapper does, so each test decides exactly
// what a user holds; everything after that goes through the real HTTP API with real cookies and tokens.
internal static class IdentityTesting
{
    public const string SessionCookie = "__Host-erp";
    public const string XsrfCookie = "XSRF-TOKEN";
    public const string XsrfHeader = "X-XSRF-TOKEN";

    /// <summary>Meets the default policy: 10+ characters with upper, lower and a digit.</summary>
    public const string Password = "Correct-horse-9";

    public static async Task<TestAccount> SeedUserAsync(
        this ApiFactory api, IEnumerable<string>? permissions = null, bool administrator = false, bool companyWide = true,
        Guid[]? branches = null, Guid[]? warehouses = null, bool active = true)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var userName = DatabaseFixture.Code("u").ToLowerInvariant();
        var user = new ApplicationUser { UserName = userName, DisplayName = "Test " + userName, IsActive = active };
        var created = await users.CreateAsync(user, Password); // tracked only: the store doesn't save on its own
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Code)));

        Guid? roleId = administrator ? ApplicationRole.AdministratorId : null;
        if (!administrator && permissions?.ToList() is { Count: > 0 } granted)
        {
            var name = DatabaseFixture.Code("role");
            var role = new ApplicationRole { Name = name, NormalizedName = name.ToUpperInvariant(), ConcurrencyStamp = Guid.NewGuid().ToString() };
            db.Add(role);
            foreach (var permission in granted)
                db.Add(new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = ErpClaimTypes.Permission, ClaimValue = permission });
            roleId = role.Id;
        }
        if (roleId is { } id)
            db.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = id });
        db.Add(UserAccess.Create(user.Id, companyWide, branches ?? [], warehouses ?? []));
        await db.SaveChangesAsync();
        return new TestAccount(user.Id, userName, Password, roleId);
    }

    /// <summary>Signs in; echoes the anti-forgery token from then on, as the React client does with XSRF-TOKEN.</summary>
    public static async Task<HttpResponseMessage> LoginAsync(this HttpClient browser, TestAccount account, string? password = null)
    {
        var response = await browser.PostJsonAsync("/api/identity/login", new { userName = account.UserName, password = password ?? account.Password });
        if (Cookie(response, XsrfCookie) is { } xsrf)
        {
            browser.DefaultRequestHeaders.Remove(XsrfHeader);
            browser.DefaultRequestHeaders.Add(XsrfHeader, xsrf);
        }
        return response;
    }

    public static async Task<HttpClient> SignedInAsync(this ApiFactory api, TestAccount account)
    {
        var browser = api.CreateBrowser();
        var response = await browser.LoginAsync(account);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return browser;
    }

    public static async Task<HttpClient> AdministratorAsync(this ApiFactory api) =>
        await api.SignedInAsync(await api.SeedUserAsync(administrator: true));

    /// <summary>The whole Set-Cookie header for this cookie, or null.</summary>
    public static string? SetCookie(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith(name + "=", StringComparison.Ordinal))
            : null;

    /// <summary>The value this response sets for the cookie, or null.</summary>
    public static string? Cookie(HttpResponseMessage response, string name) =>
        SetCookie(response, name) is { } header ? Uri.UnescapeDataString(header.Split(';')[0][(name.Length + 1)..]) : null;

    public static Task<HttpResponseMessage> SendAsBearerAsync(this HttpClient client, HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }
}
