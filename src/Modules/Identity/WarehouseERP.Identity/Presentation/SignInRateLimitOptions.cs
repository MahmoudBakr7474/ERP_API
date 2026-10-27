using System.ComponentModel.DataAnnotations;

namespace WarehouseERP.Identity.Presentation;

/// <summary>
/// One credential operation's limit, bound to <c>Identity:SignInRateLimit:&lt;operation id&gt;</c> (Login, IssueApiToken,
/// RefreshApiToken, ChangeMyPassword): how many attempts one client address may make per window. Each operation has
/// its own budget, so a busy one never spends another's. A branch's users and POS devices usually share one public
/// address (and an IPv6 LAN segment counts as one, its /64), so size each limit for the busiest such address: about
/// twice its clients that use the operation. Read once at startup; a change applies after a restart.
/// Lockout bounds sequential guessing of one account; these limits bound bursts and spraying from one address or /64.
/// Behind a reverse proxy the client address is only known with the host's ReverseProxy settings.
/// </summary>
public sealed class SignInRateLimitOptions
{
    public const string SectionName = "Identity:SignInRateLimit";

    [Range(1, 100_000)]
    public int PermitLimit { get; set; }

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
