using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Contracts.UserAccess;
using WarehouseERP.Identity.Application.Contracts.Users;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Application.Features.Account.ChangeMyPassword;
using WarehouseERP.Identity.Application.Features.Account.GetMe;
using WarehouseERP.Identity.Application.Features.Authentication.Login;
using WarehouseERP.Identity.Application.Features.Authentication.RefreshApiToken;
using WarehouseERP.Identity.Application.Features.Roles.CreateRole;
using WarehouseERP.Identity.Application.Features.Sessions.RevokeSession;
using WarehouseERP.Identity.Application.Features.Users.CreateUser;
using WarehouseERP.Identity.Application.Features.Users.SetUserAccess;
using WarehouseERP.Identity.Application.Features.Users.SetUserActive;
using WarehouseERP.Identity.Application.Features.Users.SetUserRoles;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.UnitTests.Identity;

// Identity's use cases against in-memory ports: sign-in, revocation, and the rules that keep anyone from handing
// out more than they hold.
public sealed class IdentityHandlerTests
{
    private const string WarehousesRead = "organization.warehouses.read";
    private const string ProductsRead = "catalog.products.read";

    private static readonly PermissionDefinition[] Catalog =
    [
        new(WarehousesRead, "Organization"), new(ProductsRead, "Catalog"),
        .. IdentityPermissions.All.Select(p => new PermissionDefinition(p, "Identity")),
    ];

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly FakeAccounts _accounts = new();
    private readonly FakeRoles _roles = new();
    private readonly FakeAccess _access = new();
    private readonly FakeSessions _sessions = new();
    private readonly FakePersistence _persistence = new();
    private readonly FakeOrganization _organization = new();
    private static readonly IOptions<IdentitySessionOptions> Sessions = Options.Create(new IdentitySessionOptions());

    // ---- sign-in -------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(LoginOutcome.UnknownUser)]
    [InlineData(LoginOutcome.WrongPassword)]
    [InlineData(LoginOutcome.LockedOut)]
    [InlineData(LoginOutcome.Inactive)]
    public async Task Every_refused_login_is_the_same_error_and_saves_the_attempt(LoginOutcome outcome)
    {
        _accounts.NextLogin = new LoginCheck(outcome, outcome == LoginOutcome.UnknownUser ? null : Guid.NewGuid());
        var log = new ListLogger();

        var result = await Login(log).HandleAsync(new LoginCommand("typed-name", "typed-secret"), default);

        Assert.Same(AuthenticationErrors.LoginFailed, result.Error);
        Assert.Empty(_sessions.All);
        Assert.Equal(1, _persistence.Saves); // the failed-attempt count
        var logged = Assert.Single(log.Entries);
        Assert.Equal(LogLevel.Warning, logged.Level);
        Assert.DoesNotContain("typed-name", logged.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("typed-secret", logged.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_conflict_saving_the_failed_attempt_still_answers_login_failed()
    {
        _accounts.NextLogin = new LoginCheck(LoginOutcome.WrongPassword, Guid.NewGuid());
        _persistence.NextSave = CommonErrors.ConcurrencyConflict;

        var result = await Login(new ListLogger()).HandleAsync(new LoginCommand("u", "p"), default);

        Assert.Same(AuthenticationErrors.LoginFailed, result.Error); // a 409 would reveal the user exists
    }

    [Fact]
    public async Task Login_starts_a_web_session_that_stores_only_the_tokens_hash()
    {
        var userId = Guid.NewGuid();
        _accounts.NextLogin = new LoginCheck(LoginOutcome.Succeeded, userId);

        var signIn = (await Login(new ListLogger()).HandleAsync(new LoginCommand("u", "p"), default)).Value;

        var session = Assert.Single(_sessions.All);
        Assert.Equal((userId, SessionClientType.Web), (session.UserId, session.ClientType));
        Assert.Equal(SessionTokens.Hash(signIn.Token), session.TokenHash);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime.AddHours(12), signIn.ExpiresAtUtc);
        Assert.Equal(1, _persistence.Saves);
    }

    [Fact]
    public async Task Reusing_a_refresh_token_saves_the_revocation_and_warns()
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var (refresh, refreshHash) = SessionTokens.Create();
        var session = UserSession.StartApi(Guid.NewGuid(), null,
            new IssuedApiTokens(SessionTokens.Create().Hash, now.AddMinutes(15), refreshHash, now.AddDays(14)), now, TimeSpan.FromDays(30));
        _sessions.Add(session);
        var log = new ListLogger();
        var handler = new RefreshApiTokenHandler(_sessions, _persistence, Sessions, _clock, new CapturingLogger<RefreshApiTokenHandler>(log));
        Assert.True((await handler.HandleAsync(new RefreshApiTokenCommand(refresh), default)).IsSuccess);

        var reused = await handler.HandleAsync(new RefreshApiTokenCommand(refresh), default);

        Assert.Same(SessionDomainErrors.RefreshTokenReused, reused.Error);
        Assert.True(session.IsRevoked);
        Assert.Equal(2, _persistence.Saves);
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Wrong_current_password_counts_toward_lockout_and_changes_nothing()
    {
        var caller = new FakeCaller();
        _sessions.StartWeb(caller.UserId!.Value, _clock.GetUtcNow().UtcDateTime);
        _accounts.NextPasswordChange = AuthenticationErrors.PasswordIncorrect;

        var result = await ChangePassword(caller).HandleAsync(new ChangeMyPasswordCommand("wrong", "New-Password-1", Guid.NewGuid()), default);

        Assert.Same(AuthenticationErrors.PasswordIncorrect, result.Error);
        Assert.Equal(1, _persistence.Saves);
        Assert.DoesNotContain(_sessions.All, s => s.IsRevoked);
    }

    [Fact]
    public async Task Changing_the_password_ends_every_other_session()
    {
        var caller = new FakeCaller();
        var current = _sessions.StartWeb(caller.UserId!.Value, _clock.GetUtcNow().UtcDateTime);
        var other = _sessions.StartWeb(caller.UserId.Value, _clock.GetUtcNow().UtcDateTime);

        var result = await ChangePassword(caller).HandleAsync(new ChangeMyPasswordCommand("old", "New-Password-1", current.Id), default);

        Assert.True(result.IsSuccess);
        Assert.False(current.IsRevoked);
        Assert.Equal(SessionRevokeReason.PasswordChanged, other.RevokedReason);
    }

    // ---- users ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Nobody_deactivates_themselves_or_the_last_administrator()
    {
        var caller = new FakeCaller();
        var onlyAdmin = _accounts.Add(_roles.AdministratorId);

        var self = await SetActive(caller).HandleAsync(new SetUserActiveCommand(caller.UserId!.Value, false), default);
        var last = await SetActive(caller).HandleAsync(new SetUserActiveCommand(onlyAdmin, false), default);

        Assert.Same(UserErrors.CannotDeactivateSelf, self.Error);
        Assert.Same(UserErrors.LastAdministrator, last.Error);
        Assert.Empty(_accounts.Changes);
    }

    [Fact]
    public async Task Deactivation_ends_every_session_in_the_same_save()
    {
        var user = _accounts.Add();
        var web = _sessions.StartWeb(user, _clock.GetUtcNow().UtcDateTime);

        var result = await SetActive(new FakeCaller()).HandleAsync(new SetUserActiveCommand(user, false), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(SessionRevokeReason.UserDeactivated, web.RevokedReason);
        Assert.Equal(1, _persistence.Saves);
    }

    [Fact]
    public async Task Nobody_grants_a_role_holding_permissions_they_lack()
    {
        var caller = new FakeCaller([IdentityPermissions.UsersManage, WarehousesRead]);
        var user = _accounts.Add();
        var products = _roles.Add("Products", permissions: ProductsRead);

        var toProducts = await SetRoles(caller).HandleAsync(new SetUserRolesCommand(user, [products]), default);
        var toAdministrator = await SetRoles(caller).HandleAsync(new SetUserRolesCommand(user, [_roles.AdministratorId]), default);
        var unknown = await SetRoles(caller).HandleAsync(new SetUserRolesCommand(user, [Guid.NewGuid()]), default);

        Assert.Same(PermissionErrors.ExceedsOwnPermissions, toProducts.Error);
        Assert.Same(PermissionErrors.ExceedsOwnPermissions, toAdministrator.Error);
        Assert.Same(RoleErrors.UnknownRoles, unknown.Error);
        Assert.Empty(_accounts.Changes);
    }

    [Fact]
    public async Task Nobody_manages_a_user_who_holds_more_than_they_do()
    {
        var caller = new FakeCaller([IdentityPermissions.UsersManage, WarehousesRead]);
        var administrator = _accounts.Add(_roles.AdministratorId);
        var reader = _roles.Add("Reader", permissions: WarehousesRead);

        var result = await SetRoles(caller).HandleAsync(new SetUserRolesCommand(administrator, [reader]), default);

        Assert.Same(PermissionErrors.ExceedsOwnPermissions, result.Error);
    }

    [Fact]
    public async Task The_last_administrator_keeps_the_role()
    {
        var onlyAdmin = _accounts.Add(_roles.AdministratorId);
        var reader = _roles.Add("Reader", permissions: WarehousesRead);

        var result = await SetRoles(new FakeCaller()).HandleAsync(new SetUserRolesCommand(onlyAdmin, [reader]), default);

        Assert.Same(UserErrors.LastAdministrator, result.Error);
    }

    [Fact]
    public async Task Scope_is_granted_only_from_within_the_callers_own_scope()
    {
        var (branch, inBranch) = _organization.AddWarehouse();
        var (otherBranch, outside) = _organization.AddWarehouse();
        var caller = new FakeCaller([IdentityPermissions.AccessManage], allWarehouses: false, warehouses: [inBranch]);
        _access.Add(UserAccess.Create(caller.UserId!.Value, companyWide: false, [branch], []));
        var user = _accounts.Add();
        var handler = SetAccess(caller);

        Assert.Same(UserAccessErrors.ExceedsOwnScope, (await handler.HandleAsync(new(user, true, [], []), default)).Error);
        Assert.Same(UserAccessErrors.ExceedsOwnScope, (await handler.HandleAsync(new(user, false, [], [outside]), default)).Error);
        Assert.Same(UserAccessErrors.ExceedsOwnScope, (await handler.HandleAsync(new(user, false, [otherBranch], []), default)).Error);
        Assert.Same(UserAccessErrors.UnknownWarehouses, (await handler.HandleAsync(new(user, false, [], [Guid.NewGuid()]), default)).Error);
        Assert.Equal(0, _persistence.Saves);

        Assert.True((await handler.HandleAsync(new(user, false, [branch], [inBranch]), default)).IsSuccess);
        Assert.True((await handler.HandleAsync(new(user, false, [branch], []), default)).IsSuccess);
        Assert.Equal(1, _access.ByUser[user].Revision); // created, then changed once
    }

    [Fact]
    public async Task Creating_a_user_with_scope_needs_access_manage()
    {
        var caller = new FakeCaller([IdentityPermissions.UsersManage]);
        var handler = new CreateUserHandler(caller, _accounts, _accounts, _roles, _access, _organization, _organization, Catalog, _persistence,
            NullLogger<CreateUserHandler>.Instance);

        var scoped = await handler.HandleAsync(new CreateUserCommand("u1", "U", null, "pw", [], true, [], []), default);
        var plain = await handler.HandleAsync(new CreateUserCommand("u2", "U", null, "pw", [], false, [], []), default);

        Assert.Same(UserAccessErrors.ManageRequired, scoped.Error);
        Assert.True(plain.IsSuccess);
        Assert.False(_access.ByUser[plain.Value].IsCompanyWide);
        Assert.Equal(1, _persistence.Saves); // account, roles and scope in one save
    }

    // ---- roles, sessions, me ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Nobody_creates_a_role_with_permissions_they_lack()
    {
        var caller = new FakeCaller([IdentityPermissions.RolesManage, WarehousesRead]);
        var handler = new CreateRoleHandler(caller, _roles, Catalog, _persistence, NullLogger<CreateRoleHandler>.Instance);

        var result = await handler.HandleAsync(new CreateRoleCommand("Clerk", null, [WarehousesRead, ProductsRead]), default);

        Assert.Same(PermissionErrors.ExceedsOwnPermissions, result.Error);
        Assert.Empty(_roles.Added);
    }

    [Fact]
    public async Task Another_users_session_does_not_exist_for_a_caller_without_sessions_manage()
    {
        var caller = new FakeCaller([WarehousesRead]);
        var mine = _sessions.StartWeb(caller.UserId!.Value, _clock.GetUtcNow().UtcDateTime);
        var theirs = _sessions.StartWeb(Guid.NewGuid(), _clock.GetUtcNow().UtcDateTime);
        var handler = new RevokeSessionHandler(caller, _sessions, _persistence, _clock, NullLogger<RevokeSessionHandler>.Instance);

        Assert.Same(SessionErrors.NotFound, (await handler.HandleAsync(new RevokeSessionCommand(theirs.Id), default)).Error);
        Assert.True((await handler.HandleAsync(new RevokeSessionCommand(mine.Id), default)).IsSuccess);

        Assert.False(theirs.IsRevoked);
        Assert.Equal(SessionRevokeReason.RevokedByUser, mine.RevokedReason);
    }

    [Fact]
    public async Task Me_lists_the_permissions_the_caller_holds_from_the_catalog()
    {
        var reader = new FakeCaller([WarehousesRead, "not.in.catalog"], allWarehouses: false, warehouses: [Guid.NewGuid()]);
        var store = new SingleUserStore(reader.UserId!.Value);

        var me = (await new GetMeHandler(reader, store, Catalog).HandleAsync(new GetMeQuery(), default)).Value;
        var admin = (await new GetMeHandler(new FakeCaller(), new SingleUserStore(Guid.Empty, any: true), Catalog)
            .HandleAsync(new GetMeQuery(), default)).Value;

        Assert.Equal([WarehousesRead], me.Permissions);
        Assert.Equal(reader.WarehouseIds.Single(), Assert.Single(me.WarehouseIds));
        Assert.Equal(Catalog.Length, admin.Permissions.Count);
        Assert.True(admin.IsCompanyWide);
    }

    // ---- wiring --------------------------------------------------------------------------------------------------

    private LoginHandler Login(ListLogger log) =>
        new(_accounts, _sessions, _persistence, Sessions, _clock, new CapturingLogger<LoginHandler>(log));

    private ChangeMyPasswordHandler ChangePassword(ICurrentUser caller) =>
        new(caller, _accounts, _sessions, _persistence, _clock, NullLogger<ChangeMyPasswordHandler>.Instance);

    private SetUserActiveHandler SetActive(ICurrentUser caller) =>
        new(caller, _accounts, _accounts, _roles, _sessions, Catalog, _persistence, _clock, NullLogger<SetUserActiveHandler>.Instance);

    private SetUserRolesHandler SetRoles(ICurrentUser caller) =>
        new(caller, _accounts, _accounts, _roles, Catalog, _persistence, NullLogger<SetUserRolesHandler>.Instance);

    private SetUserAccessHandler SetAccess(ICurrentUser caller) =>
        new(caller, _accounts, _accounts, _roles, _access, _organization, _organization, Catalog, _persistence, NullLogger<SetUserAccessHandler>.Instance);

    private sealed class CapturingLogger<T>(ListLogger inner) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            inner.Log(logLevel, eventId, state, exception, formatter);
    }

    private sealed class SingleUserStore(Guid userId, bool any = false) : IUserReadStore
    {
        public Task<UserDetailsResponse?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<UserDetailsResponse?>(any || id == userId
            ? new UserDetailsResponse(id, "me", "Me", null, true, false, [], new UserAccessResponse(false, [], []))
            : null);

        public Task<WarehouseERP.BuildingBlocks.Application.Pagination.PagedResult<UserSummaryResponse>> ListAsync(
            string? search, int page, int pageSize, CancellationToken ct) => throw new NotSupportedException();
    }
}
