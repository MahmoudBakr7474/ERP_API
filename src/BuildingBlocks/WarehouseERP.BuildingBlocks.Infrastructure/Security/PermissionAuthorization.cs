using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Security;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Security;

/// <summary>The user holds this permission, or every permission.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && (context.User.HasClaim(ErpClaimTypes.Permission, requirement.Permission)
                || context.User.HasClaim(ErpClaimTypes.Permission, ErpClaimTypes.AllPermissions)))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

// Policy name = permission name: RequireAuthorization(CatalogPermissions.ProductsRead) works for any
// permission a module registered, without the host listing them.
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options, IEnumerable<PermissionDefinition> permissions)
    : DefaultAuthorizationPolicyProvider(options)
{
    private readonly HashSet<string> _known = permissions.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName) =>
        await base.GetPolicyAsync(policyName) ?? (_known.Contains(policyName)
            ? new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(policyName)).Build()
            : null);
}

public static class PermissionRegistration
{
    /// <summary>Adds a module's permissions to the catalog.</summary>
    public static IServiceCollection AddPermissions(this IServiceCollection services, string module, IEnumerable<string> names)
    {
        foreach (var name in names)
            services.AddSingleton(new PermissionDefinition(name, module));
        return services;
    }

    /// <summary>Permission policies, deny-by-default fallback (an endpoint opts out with AllowAnonymous).</summary>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }
}
