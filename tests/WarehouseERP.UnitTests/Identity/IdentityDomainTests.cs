using System.Security.Cryptography;
using System.Text;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.UnitTests.Identity;

public sealed class IdentityDomainTests
{
    private static readonly IReadOnlySet<string> Known = new HashSet<string> { "catalog.products.read", "organization.warehouses.read" };
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    // ---- Role ----------------------------------------------------------------------------------------------------

    [Fact]
    public void Role_refuses_permissions_outside_the_catalog_and_names_them()
    {
        var result = Role.Create("Clerk", null, ["catalog.products.read", "no.such", "also.missing"], Known);

        Assert.Equal("Identity.Role.UnknownPermission", result.Error!.Code);
        Assert.Contains("also.missing, no.such", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void System_role_can_not_be_edited_or_deleted_and_only_administrator_grants_everything()
    {
        var administrator = Role.Restore(Guid.NewGuid(), Role.AdministratorName, null, isSystem: true, []);
        var lookalike = Role.Create(Role.AdministratorName + "s", null, [], Known).Value;

        Assert.Same(RoleDomainErrors.SystemRoleLocked, administrator.Update("x", null, [], Known).Error);
        Assert.Same(RoleDomainErrors.SystemRoleLocked, administrator.EnsureDeletable().Error);
        Assert.True(administrator.GrantsAllPermissions);
        Assert.False(lookalike.GrantsAllPermissions);
        Assert.True(lookalike.EnsureDeletable().IsSuccess);
    }

    [Fact]
    public void Role_update_replaces_name_description_and_permissions()
    {
        var role = Role.Create(" Clerk ", " ", ["catalog.products.read"], Known).Value;

        Assert.True(role.Update(" Storekeeper ", " Main store ", ["organization.warehouses.read"], Known).IsSuccess);

        Assert.Equal(("Storekeeper", "Main store"), (role.Name, role.Description));
        Assert.Equal(["organization.warehouses.read"], role.Permissions);
    }

    // ---- UserAccess ----------------------------------------------------------------------------------------------

    [Fact]
    public void Company_wide_access_drops_branch_and_warehouse_grants()
    {
        var access = UserAccess.Create(Guid.NewGuid(), companyWide: false, [Guid.NewGuid()], [Guid.NewGuid()]);

        access.Set(companyWide: true, [Guid.NewGuid()], [Guid.NewGuid()]);

        Assert.True(access.IsCompanyWide);
        Assert.Empty(access.Branches);
        Assert.Empty(access.Warehouses);
    }

    [Fact]
    public void Setting_access_keeps_unchanged_grants_and_bumps_the_revision()
    {
        var (kept, dropped, added) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var access = UserAccess.Create(Guid.NewGuid(), companyWide: false, [kept, dropped], []);
        var keptGrant = access.Branches.Single(b => b.BranchId == kept);

        access.Set(companyWide: false, [kept, added, added, Guid.Empty], []);

        Assert.Equal(1, access.Revision);
        Assert.Equal(new[] { kept, added }.Order(), access.Branches.Select(b => b.BranchId).Order());
        Assert.Same(keptGrant, access.Branches.Single(b => b.BranchId == kept)); // not deleted and re-added
    }

    // ---- UserSession ---------------------------------------------------------------------------------------------

    [Fact]
    public void Web_session_slides_until_idle_and_ends_at_its_absolute_expiry()
    {
        var session = UserSession.StartWeb(Guid.NewGuid(), SessionTokens.Create().Hash, Now, TimeSpan.FromHours(12));
        var idle = TimeSpan.FromHours(8);

        Assert.True(session.AcceptsToken(Now.AddHours(7), idle));
        Assert.False(session.AcceptsToken(Now.AddHours(8), idle));
        Assert.False(session.AcceptsToken(Now.AddHours(12), TimeSpan.FromDays(1)));
    }

    [Fact]
    public void Revoked_session_accepts_nothing_and_keeps_its_first_reason()
    {
        var session = UserSession.StartWeb(Guid.NewGuid(), SessionTokens.Create().Hash, Now, TimeSpan.FromHours(12));

        session.Revoke(SessionRevokeReason.Logout, Now.AddMinutes(1));
        session.Revoke(SessionRevokeReason.RevokedByAdministrator, Now.AddMinutes(2));

        Assert.False(session.AcceptsToken(Now.AddMinutes(3), TimeSpan.FromHours(8)));
        Assert.Equal((SessionRevokeReason.Logout, Now.AddMinutes(1)), (session.RevokedReason, session.RevokedAtUtc));
    }

    [Fact]
    public void Api_access_token_expires_on_its_own_and_refresh_rotates_both_tokens()
    {
        var (first, refresh1) = Issue(Now);
        var session = UserSession.StartApi(Guid.NewGuid(), " POS-01 ", first, Now, TimeSpan.FromDays(30));
        Assert.Equal("POS-01", session.DeviceId);
        Assert.False(session.AcceptsToken(Now.AddMinutes(15), TimeSpan.FromHours(8)));

        var (second, _) = Issue(Now.AddMinutes(20));
        Assert.True(session.Refresh(SessionTokens.Hash(refresh1), second, Now.AddMinutes(20)).IsSuccess);

        Assert.Equal(second.AccessTokenHash, session.TokenHash);
        Assert.Equal(first.RefreshTokenHash, session.PreviousRefreshTokenHash);
        Assert.True(session.AcceptsToken(Now.AddMinutes(21), TimeSpan.FromHours(8)));
    }

    [Fact]
    public void Reusing_a_rotated_refresh_token_ends_the_session()
    {
        var (first, refresh1) = Issue(Now);
        var session = UserSession.StartApi(Guid.NewGuid(), null, first, Now, TimeSpan.FromDays(30));
        session.Refresh(SessionTokens.Hash(refresh1), Issue(Now).Tokens, Now.AddMinutes(1));

        var reused = session.Refresh(SessionTokens.Hash(refresh1), Issue(Now).Tokens, Now.AddMinutes(2));

        Assert.Same(SessionDomainErrors.RefreshTokenReused, reused.Error);
        Assert.Equal(SessionRevokeReason.RefreshTokenReused, session.RevokedReason);
    }

    [Fact]
    public void Refresh_is_refused_for_an_unknown_or_expired_token_or_a_web_session()
    {
        var (tokens, refresh) = Issue(Now);
        var api = UserSession.StartApi(Guid.NewGuid(), null, tokens, Now, TimeSpan.FromDays(30));
        var web = UserSession.StartWeb(Guid.NewGuid(), SessionTokens.Create().Hash, Now, TimeSpan.FromHours(12));

        Assert.Same(SessionDomainErrors.Revoked, api.Refresh(SessionTokens.Create().Hash, Issue(Now).Tokens, Now).Error);
        Assert.Same(SessionDomainErrors.Revoked, api.Refresh(SessionTokens.Hash(refresh), Issue(Now).Tokens, Now.AddDays(15)).Error);
        Assert.Same(SessionDomainErrors.Revoked, web.Refresh(SessionTokens.Hash(refresh), Issue(Now).Tokens, Now).Error);
        Assert.Null(api.RevokedAtUtc); // a wrong token alone doesn't end the session
    }

    [Fact]
    public void Tokens_are_256_bit_base64url_and_only_their_sha256_is_kept()
    {
        var (token, hash) = SessionTokens.Create();

        Assert.Equal(43, token.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", token);
        Assert.Equal(SHA256.HashData(Encoding.UTF8.GetBytes(token)), hash);
        Assert.True(SessionTokens.HashesEqual(hash, SessionTokens.Hash(token)));
        Assert.NotEqual(token, SessionTokens.Create().Token);
    }

    private static (IssuedApiTokens Tokens, string RefreshToken) Issue(DateTime now)
    {
        var (_, accessHash) = SessionTokens.Create();
        var (refresh, refreshHash) = SessionTokens.Create();
        return (new IssuedApiTokens(accessHash, now.AddMinutes(15), refreshHash, now.AddDays(14)), refresh);
    }
}
