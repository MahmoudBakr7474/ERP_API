namespace WarehouseERP.Identity;

// How sessions travel over HTTP; shared by the authentication handler and the endpoints.
internal static class IdentityHttpDefaults
{
    public const string Scheme = "ErpSession";

    /// <summary>The web session token: HttpOnly, Secure, SameSite=Strict, host-only (<c>__Host-</c>).</summary>
    public const string SessionCookie = "__Host-erp";

    /// <summary>ASP.NET Core anti-forgery's own HttpOnly cookie.</summary>
    public const string AntiforgeryCookie = "__Host-erp-af";

    /// <summary>The anti-forgery request token, readable by the React client, echoed in <see cref="XsrfHeader"/>.</summary>
    public const string XsrfCookie = "XSRF-TOKEN";

    public const string XsrfHeader = "X-XSRF-TOKEN";

    // The principal's authentication type says how the session arrived; only cookies need anti-forgery checks.
    public const string CookieAuthenticationType = "ErpSessionCookie";
    public const string BearerAuthenticationType = "ErpSessionBearer";
}
