using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.BuildingBlocks.Infrastructure.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Security;
using WarehouseERP.Identity.Application;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Application.Features.ChangeMyPassword;
using WarehouseERP.Identity.Application.Features.CreateRole;
using WarehouseERP.Identity.Application.Features.CreateUser;
using WarehouseERP.Identity.Application.Features.DeleteRole;
using WarehouseERP.Identity.Application.Features.GetMe;
using WarehouseERP.Identity.Application.Features.GetRole;
using WarehouseERP.Identity.Application.Features.GetUser;
using WarehouseERP.Identity.Application.Features.IssueApiToken;
using WarehouseERP.Identity.Application.Features.ListPermissions;
using WarehouseERP.Identity.Application.Features.ListRoles;
using WarehouseERP.Identity.Application.Features.ListSessions;
using WarehouseERP.Identity.Application.Features.ListUsers;
using WarehouseERP.Identity.Application.Features.Login;
using WarehouseERP.Identity.Application.Features.Logout;
using WarehouseERP.Identity.Application.Features.RefreshApiToken;
using WarehouseERP.Identity.Application.Features.ResetUserPassword;
using WarehouseERP.Identity.Application.Features.RevokeSession;
using WarehouseERP.Identity.Application.Features.RevokeUserSessions;
using WarehouseERP.Identity.Application.Features.SetUserAccess;
using WarehouseERP.Identity.Application.Features.SetUserActive;
using WarehouseERP.Identity.Application.Features.SetUserRoles;
using WarehouseERP.Identity.Application.Features.UnlockUser;
using WarehouseERP.Identity.Application.Features.UpdateRole;
using WarehouseERP.Identity.Application.Features.UpdateUser;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.Identity.Infrastructure;
using WarehouseERP.Identity.Infrastructure.Persistence;
using WarehouseERP.Identity.Infrastructure.Security;
using WarehouseERP.Identity.Presentation;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity;

// Users, roles, permissions, scope and sessions; authenticates every request and provides ICurrentUser to all
// modules. The host calls AddIdentityModule, UseIdentityModule (after UseAuthentication) and MapIdentityEndpoints.
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginValidator>(includeInternalTypes: true);
        AddHandlers(services);
        AddOptions(services);

        // ASP.NET Core Identity: UserManager, password hashing and policy, lockout. The store never saves on its
        // own (AutoSaveChanges off): every use case commits once through IIdentityPersistence.
        services.AddIdentityCore<ApplicationUser>();
        services.AddScoped<IUserStore<ApplicationUser>>(sp =>
            new UserStore<ApplicationUser, ApplicationRole, DbContext, Guid>(
                sp.GetRequiredService<DbContext>(), sp.GetService<IdentityErrorDescriber>())
            {
                AutoSaveChanges = false,
            });

        services.AddScoped<IUserAccounts, UserAccounts>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserAccessRepository, UserAccessRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IUserReadStore, UserReadStore>();
        services.AddScoped<IIdentityPersistence, IdentityPersistence>();
        services.AddScoped<IUserDirectory, UserDirectory>();

        // Authentication for every module: server-side sessions, presented as a cookie (web) or a bearer token
        // (POS/API). Authorization: one policy per registered permission, deny by default.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<AccessResolver>();
        services.AddAuthentication(IdentityHttpDefaults.Scheme)
            .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(IdentityHttpDefaults.Scheme, null);
        services.AddPermissionAuthorization();
        services.AddAntiforgery(o =>
        {
            o.HeaderName = IdentityHttpDefaults.XsrfHeader;
            o.Cookie.Name = IdentityHttpDefaults.AntiforgeryCookie;
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Cookie.SameSite = SameSiteMode.Strict;
        });

        services.AddHostedService<IdentityBootstrapper>();
        services.AddPermissions("Identity", IdentityPermissions.All);
        return services;
    }

    /// <summary>Anti-forgery check for cookie sessions; place it after UseAuthentication.</summary>
    public static IApplicationBuilder UseIdentityModule(this IApplicationBuilder app) =>
        app.UseMiddleware<CookieSessionAntiforgeryMiddleware>();

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app) =>
        app.MapAuthEndpoints().MapUserEndpoints().MapRoleEndpoints();

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddCommandHandler<LoginCommand, WebSignIn, LoginHandler>();
        services.AddCommandHandler<LogoutCommand, Done, LogoutHandler>();
        services.AddCommandHandler<IssueApiTokenCommand, ApiTokens, IssueApiTokenHandler>();
        services.AddCommandHandler<RefreshApiTokenCommand, ApiTokens, RefreshApiTokenHandler>();
        services.AddCommandHandler<ChangeMyPasswordCommand, Done, ChangeMyPasswordHandler>();
        services.AddQueryHandler<GetMeQuery, MeResponse, GetMeHandler>();

        services.AddCommandHandler<CreateUserCommand, Guid, CreateUserHandler>();
        services.AddCommandHandler<UpdateUserCommand, Done, UpdateUserHandler>();
        services.AddCommandHandler<SetUserActiveCommand, Done, SetUserActiveHandler>();
        services.AddCommandHandler<ResetUserPasswordCommand, Done, ResetUserPasswordHandler>();
        services.AddCommandHandler<UnlockUserCommand, Done, UnlockUserHandler>();
        services.AddCommandHandler<SetUserRolesCommand, Done, SetUserRolesHandler>();
        services.AddCommandHandler<SetUserAccessCommand, Done, SetUserAccessHandler>();
        services.AddQueryHandler<GetUserQuery, UserDetails, GetUserHandler>();
        services.AddQueryHandler<ListUsersQuery, PagedResult<UserSummary>, ListUsersHandler>();

        services.AddCommandHandler<CreateRoleCommand, Guid, CreateRoleHandler>();
        services.AddCommandHandler<UpdateRoleCommand, Done, UpdateRoleHandler>();
        services.AddCommandHandler<DeleteRoleCommand, Done, DeleteRoleHandler>();
        services.AddQueryHandler<GetRoleQuery, RoleDetails, GetRoleHandler>();
        services.AddQueryHandler<ListRolesQuery, IReadOnlyList<RoleSummary>, ListRolesHandler>();
        services.AddQueryHandler<ListPermissionsQuery, IReadOnlyList<PermissionDefinition>, ListPermissionsHandler>();

        services.AddQueryHandler<ListSessionsQuery, IReadOnlyList<SessionItem>, ListSessionsHandler>();
        services.AddCommandHandler<RevokeSessionCommand, Done, RevokeSessionHandler>();
        services.AddCommandHandler<RevokeUserSessionsCommand, Done, RevokeUserSessionsHandler>();
    }

    private static void AddOptions(IServiceCollection services)
    {
        services.AddOptions<IdentitySessionOptions>()
            .BindConfiguration(IdentitySessionOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(o => o.WebIdleTimeout <= o.WebAbsoluteLifetime,
                "Identity:Sessions:WebIdleTimeout must not exceed WebAbsoluteLifetime.")
            .Validate(o => o.ApiAccessTokenLifetime <= o.ApiRefreshTokenLifetime && o.ApiRefreshTokenLifetime <= o.ApiAbsoluteLifetime,
                "Identity:Sessions: ApiAccessTokenLifetime <= ApiRefreshTokenLifetime <= ApiAbsoluteLifetime.")
            .ValidateOnStart();

        services.AddOptions<IdentityBootstrapOptions>()
            .BindConfiguration(IdentityBootstrapOptions.SectionName)
            .Validate(o => o.IsConfigured || (string.IsNullOrWhiteSpace(o.UserName) && string.IsNullOrEmpty(o.Password)),
                "Identity:Bootstrap needs both UserName and Password, or neither.")
            .ValidateOnStart();

        // Password policy and lockout, from Identity:Password and Identity:Lockout. Only these two sections bind:
        // Identity's store options shape the database model and must stay at their defaults.
        services.AddOptions<IdentityOptions>()
            .Configure<IConfiguration>((o, configuration) =>
            {
                o.Password.RequiredLength = 10;
                o.Password.RequireNonAlphanumeric = false;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                o.Lockout.AllowedForNewUsers = true;
                o.User.RequireUniqueEmail = false;
                configuration.GetSection("Identity:Password").Bind(o.Password);
                configuration.GetSection("Identity:Lockout").Bind(o.Lockout);
            })
            .Validate(o => o.Password.RequiredLength >= 8, "Identity:Password:RequiredLength must be at least 8.")
            .Validate(o => o.Lockout.AllowedForNewUsers && o.Lockout.MaxFailedAccessAttempts is >= 3 and <= 20
                           && o.Lockout.DefaultLockoutTimeSpan >= TimeSpan.FromMinutes(1),
                "Identity:Lockout must stay on: 3 to 20 attempts, at least one minute.")
            .ValidateOnStart();
    }
}
