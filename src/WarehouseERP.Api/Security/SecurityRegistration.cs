using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Api.Security;

/// <summary>
/// Bound to <c>Authentication:Jwt</c>. Present only when a trusted external issuer exists: the API
/// validates tokens, it never issues them.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    /// <summary>The issuer's base URL; signing keys come from its OpenID Connect metadata.</summary>
    [Required, Url]
    public string Authority { get; init; } = "";

    [Required]
    public string Audience { get; init; } = "";

    public bool RequireHttpsMetadata { get; init; } = true;
}

public static class SecurityRegistration
{
    /// <summary>Scheme used while no identity provider is configured: nobody authenticates.</summary>
    public const string NoIdentityProviderScheme = "NoIdentityProvider";

    // Authentication is identity-provider agnostic: handlers see ICurrentUser, endpoints see
    // permission policies. Without Authentication:Jwt every protected endpoint answers 401.
    public static IServiceCollection AddErpSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        if (configuration.GetSection(JwtOptions.SectionName).Exists())
        {
            services.AddOptions<JwtOptions>()
                .BindConfiguration(JwtOptions.SectionName)
                .ValidateDataAnnotations()
                .Validate(o => !o.RequireHttpsMetadata || o.Authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase),
                    "Authentication:Jwt:Authority must use https unless RequireHttpsMetadata is false.")
                .ValidateOnStart();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
                {
                    bearer.Authority = jwt.Value.Authority;
                    bearer.Audience = jwt.Value.Audience;
                    bearer.RequireHttpsMetadata = jwt.Value.RequireHttpsMetadata;
                    bearer.MapInboundClaims = false; // keep "sub", "permission", "warehouse" as issued
                });
        }
        else
        {
            services.AddAuthentication(NoIdentityProviderScheme)
                .AddScheme<AuthenticationSchemeOptions, NoIdentityProviderHandler>(NoIdentityProviderScheme, null);
        }

        services.AddAuthorizationBuilder()
            // Deny by default: an endpoint has to opt out (AllowAnonymous) or name a permission.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPermissionPolicies(OrganizationPermissions.All)
            .AddPermissionPolicies(CatalogPermissions.All);
        return services;
    }

    // Policy name = permission name, so endpoints write RequireAuthorization(CatalogPermissions.ProductsRead).
    private static AuthorizationBuilder AddPermissionPolicies(this AuthorizationBuilder builder, IEnumerable<string> permissions)
    {
        foreach (var permission in permissions)
            builder.AddPolicy(permission, p => p.RequireAuthenticatedUser().RequireClaim(ErpClaimTypes.Permission, permission));
        return builder;
    }
}

internal sealed class NoIdentityProviderHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
}
