using System.ComponentModel.DataAnnotations;

namespace WarehouseERP.Identity.Application;

/// <summary>Bound to <c>Identity:Sessions</c>; validated at startup.</summary>
public sealed class IdentitySessionOptions
{
    public const string SectionName = "Identity:Sessions";

    /// <summary>A web session ends after this long without a request.</summary>
    [Range(typeof(TimeSpan), "00:05:00", "1.00:00:00")]
    public TimeSpan WebIdleTimeout { get; init; } = TimeSpan.FromHours(8);

    /// <summary>A web session ends this long after sign-in, used or not.</summary>
    [Range(typeof(TimeSpan), "00:05:00", "7.00:00:00")]
    public TimeSpan WebAbsoluteLifetime { get; init; } = TimeSpan.FromHours(12);

    /// <summary>Lifetime of an API (POS) access token; a refresh renews it.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "01:00:00")]
    public TimeSpan ApiAccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>Lifetime of an API refresh token; each refresh issues a new one.</summary>
    [Range(typeof(TimeSpan), "00:05:00", "90.00:00:00")]
    public TimeSpan ApiRefreshTokenLifetime { get; init; } = TimeSpan.FromDays(14);

    /// <summary>An API session ends this long after sign-in, however often it refreshes.</summary>
    [Range(typeof(TimeSpan), "00:05:00", "180.00:00:00")]
    public TimeSpan ApiAbsoluteLifetime { get; init; } = TimeSpan.FromDays(30);
}
