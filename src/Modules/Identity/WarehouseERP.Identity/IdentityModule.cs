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
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.UserAccess;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Contracts.Authentication;
using WarehouseERP.Identity.Application.Contracts.Roles;
using WarehouseERP.Identity.Application.Contracts.SecurityEvents;
using WarehouseERP.Identity.Application.Contracts.Sessions;
using WarehouseERP.Identity.Application.Contracts.Users;
using WarehouseERP.Identity.Application.Features.Account.ChangeMyPassword;
using WarehouseERP.Identity.Application.Features.Account.GetMe;
using WarehouseERP.Identity.Application.Features.Account.GetPasswordPolicy;
using WarehouseERP.Identity.Application.Features.Authentication.IssueApiToken;
using WarehouseERP.Identity.Application.Features.Authentication.Login;
using WarehouseERP.Identity.Application.Features.Authentication.Logout;
using WarehouseERP.Identity.Application.Features.Authentication.RefreshApiToken;
using WarehouseERP.Identity.Application.Features.Permissions.ListPermissions;
using WarehouseERP.Identity.Application.Features.Roles.CreateRole;
using WarehouseERP.Identity.Application.Features.Roles.DeleteRole;
using WarehouseERP.Identity.Application.Features.Roles.GetRole;
using WarehouseERP.Identity.Application.Features.Roles.ListRoles;
using WarehouseERP.Identity.Application.Features.Roles.UpdateRole;
using WarehouseERP.Identity.Application.Features.SecurityEvents.ListSecurityEvents;
using WarehouseERP.Identity.Application.Features.Sessions.ListSessions;
using WarehouseERP.Identity.Application.Features.Sessions.RevokeSession;
using WarehouseERP.Identity.Application.Features.Sessions.RevokeUserSessions;
using WarehouseERP.Identity.Application.Features.Users.CreateUser;
using WarehouseERP.Identity.Application.Features.Users.GetUser;
using WarehouseERP.Identity.Application.Features.Users.ListUsers;
using WarehouseERP.Identity.Application.Features.Users.ResetUserPassword;
using WarehouseERP.Identity.Application.Features.Users.SetUserAccess;
using WarehouseERP.Identity.Application.Features.Users.SetUserActive;
using WarehouseERP.Identity.Application.Features.Users.SetUserRoles;
using WarehouseERP.Identity.Application.Features.Users.UnlockUser;
using WarehouseERP.Identity.Application.Features.Users.UpdateUser;
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
        services.AddScoped<IUserCredentials, UserCredentials>();
        services.AddScoped<IUserRoleAssignments, UserRoleAssignments>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserAccessRepository, UserAccessRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IUserReadStore, UserReadStore>();
        services.AddScoped<IIdentityPersistence, IdentityPersistence>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<ISecurityEventLog, SecurityEventLog>();
        services.AddScoped<ISecurityEventReadStore, SecurityEventReadStore>();
        services.AddSingleton<IPasswordPolicy, PasswordPolicy>();

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

        services.AddSignInRateLimit();

        services.AddHostedService<IdentityBootstrapper>();
        services.AddSingleton<ExpiredSessionCleanup>();
        services.AddHostedService(sp => sp.GetRequiredService<ExpiredSessionCleanup>());
        services.AddPermissions("Identity", IdentityPermissions.All);
        return services;
    }

    /// <summary>Anti-forgery check for cookie sessions; place it after UseAuthentication.</summary>
    public static IApplicationBuilder UseIdentityModule(this IApplicationBuilder app) =>
        app.UseMiddleware<CookieSessionAntiforgeryMiddleware>();

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app) =>
        app.MapAuthEndpoints().MapUserEndpoints().MapRoleEndpoints().MapSecurityEventEndpoints();

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddCommandHandler<LoginCommand, LoginResponse, LoginHandler>();
        services.AddCommandHandler<LogoutCommand, Done, LogoutHandler>();
        services.AddCommandHandler<IssueApiTokenCommand, ApiTokensResponse, IssueApiTokenHandler>();
        services.AddCommandHandler<RefreshApiTokenCommand, ApiTokensResponse, RefreshApiTokenHandler>();
        services.AddCommandHandler<ChangeMyPasswordCommand, Done, ChangeMyPasswordHandler>();
        services.AddQueryHandler<GetMeQuery, MeResponse, GetMeHandler>();
        services.AddQueryHandler<GetPasswordPolicyQuery, PasswordPolicyResponse, GetPasswordPolicyHandler>();

        services.AddCommandHandler<CreateUserCommand, Guid, CreateUserHandler>();
        services.AddCommandHandler<UpdateUserCommand, Done, UpdateUserHandler>();
        services.AddCommandHandler<SetUserActiveCommand, Done, SetUserActiveHandler>();
        services.AddCommandHandler<ResetUserPasswordCommand, Done, ResetUserPasswordHandler>();
        services.AddCommandHandler<UnlockUserCommand, Done, UnlockUserHandler>();
        services.AddCommandHandler<SetUserRolesCommand, Done, SetUserRolesHandler>();
        services.AddCommandHandler<SetUserAccessCommand, Done, SetUserAccessHandler>();
        services.AddQueryHandler<GetUserQuery, UserDetailsResponse, GetUserHandler>();
        services.AddQueryHandler<ListUsersQuery, PagedResult<UserSummaryResponse>, ListUsersHandler>();

        services.AddCommandHandler<CreateRoleCommand, Guid, CreateRoleHandler>();
        services.AddCommandHandler<UpdateRoleCommand, Done, UpdateRoleHandler>();
        services.AddCommandHandler<DeleteRoleCommand, Done, DeleteRoleHandler>();
        services.AddQueryHandler<GetRoleQuery, RoleDetailsResponse, GetRoleHandler>();
        services.AddQueryHandler<ListRolesQuery, IReadOnlyList<RoleSummaryResponse>, ListRolesHandler>();
        services.AddQueryHandler<ListPermissionsQuery, IReadOnlyList<PermissionDefinition>, ListPermissionsHandler>();

        services.AddQueryHandler<ListSessionsQuery, IReadOnlyList<SessionResponse>, ListSessionsHandler>();
        services.AddCommandHandler<RevokeSessionCommand, Done, RevokeSessionHandler>();
        services.AddCommandHandler<RevokeUserSessionsCommand, Done, RevokeUserSessionsHandler>();

        services.AddQueryHandler<ListSecurityEventsQuery, PagedResult<SecurityEventResponse>, ListSecurityEventsHandler>();
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
