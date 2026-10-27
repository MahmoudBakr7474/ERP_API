using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Security;

// Authenticates every request against identity.UserSessions: the web cookie or a bearer access token is hashed and
// looked up, so a revoked, expired or idle session, or a deactivated user, is refused on the very next request.
// The principal carries the user and session ids plus the access resolved for this request, as the claims
// ICurrentUser and the permission policies read.
internal sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory loggerFactory, UrlEncoder encoder,
    DbContext db, AccessResolver accessResolver, IOptions<IdentitySessionOptions> sessionOptions, TimeProvider clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    // Our tokens are 43 characters; anything far longer is not one of them and is not worth hashing.
    private const int MaxTokenLength = 128;

    // A session's last-seen time (the web idle timeout) is written at most this often, not on every request.
    private static readonly TimeSpan TouchInterval = TimeSpan.FromMinutes(1);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (ReadToken() is not { } presented)
            return AuthenticateResult.NoResult();

        var (token, transport) = presented;
        var ct = Context.RequestAborted;
        var hash = SessionTokens.Hash(token);
        var found = await db.Set<UserSession>().AsNoTracking()
            .Where(s => s.TokenHash == hash)
            .Join(db.Set<ApplicationUser>(), s => s.UserId, u => u.Id, (s, u) => new { Session = s, u.IsActive })
            .SingleOrDefaultAsync(ct);
        var now = clock.GetUtcNow().UtcDateTime;
        // A web token only counts in the cookie and an API token only as a bearer token.
        if (found is null || found.Session.ClientType != transport || !found.IsActive
            || !found.Session.AcceptsToken(now, sessionOptions.Value.WebIdleTimeout))
            return AuthenticateResult.Fail("The session is unknown, expired or revoked.");

        var session = found.Session;
        if (now - session.LastSeenAtUtc >= TouchInterval)
            await db.Set<UserSession>()
                .Where(s => s.Id == session.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSeenAtUtc, now), ct);

        var access = await accessResolver.ResolveAsync(session.UserId, ct);
        var authenticationType = transport == SessionClientType.Web
            ? IdentityHttpDefaults.CookieAuthenticationType
            : IdentityHttpDefaults.BearerAuthenticationType;
        var identity = new ClaimsIdentity(Claims(session, access), authenticationType, ErpClaimTypes.Subject, roleType: null);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    private (string Token, SessionClientType Transport)? ReadToken()
    {
        // A token sent over plain HTTP is not accepted (behind a TLS-terminating proxy: see ReverseProxy settings), so a
        // POS configured with http:// fails at once instead of working while its tokens travel in clear text.
        if (!Request.IsHttps)
            return null;
        const string bearer = "Bearer ";
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith(bearer, StringComparison.OrdinalIgnoreCase))
        {
            var token = authorization[bearer.Length..].Trim();
            return token.Length is > 0 and <= MaxTokenLength ? (token, SessionClientType.Api) : null;
        }
        return Request.Cookies.TryGetValue(IdentityHttpDefaults.SessionCookie, out var cookie) && cookie.Length is > 0 and <= MaxTokenLength
            ? (cookie, SessionClientType.Web)
            : null;
    }

    private static IEnumerable<Claim> Claims(UserSession session, EffectiveAccess access)
    {
        yield return new Claim(ErpClaimTypes.Subject, session.UserId.ToString());
        yield return new Claim(ErpClaimTypes.SessionId, session.Id.ToString());
        if (access.AllPermissions)
            yield return new Claim(ErpClaimTypes.Permission, ErpClaimTypes.AllPermissions);
        foreach (var permission in access.Permissions)
            yield return new Claim(ErpClaimTypes.Permission, permission);
        if (access.AllWarehouses)
            yield return new Claim(ErpClaimTypes.Warehouse, ErpClaimTypes.AllWarehouses);
        foreach (var warehouseId in access.WarehouseIds)
            yield return new Claim(ErpClaimTypes.Warehouse, warehouseId.ToString("D", CultureInfo.InvariantCulture));
    }
}
