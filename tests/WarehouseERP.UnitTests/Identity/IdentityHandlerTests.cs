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
using WarehouseERP.Identity.Application.Features.Roles.UpdateRole;
using WarehouseERP.Identity.Application.Features.Sessions.RevokeSession;
using WarehouseERP.Identity.Application.Features.Users.CreateUser;
using WarehouseERP.Identity.Application.Features.Users.ResetUserPassword;
using WarehouseERP.Identity.Application.Features.Users.SetUserAccess;
using WarehouseERP.Identity.Application.Features.Users.SetUserActive;
using WarehouseERP.Identity.Application.Features.Users.SetUserRoles;
using WarehouseERP.Identity.Application.Features.Users.UnlockUser;
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
    private readonly FakeAccounts _accounts;
    private readonly FakeRoles _roles = new();
    private readonly FakeAccess _access = new();
    private readonly FakeSessions _sessions = new();
    private readonly FakePersistence _persistence = new();
    private readonly FakeOrganization _organization = new();
    private readonly FakeSecurityEvents _events = new();
    private static readonly IOptions<IdentitySessionOptions> Sessions = Options.Create(new IdentitySessionOptions());

    public IdentityHandlerTests() => _accounts = new FakeAccounts(_access);

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
        Assert.Equal(1, _persistence.Saves); // the security event only; a count was already written, atomically, by IUserCredentials
        var recorded = Assert.Single(_events.Recorded);
        Assert.Equal((SecurityEventType.SignInFailed, _accounts.NextLogin.UserId), (recorded.Type, recorded.TargetUserId));
        Assert.DoesNotContain("typed", System.Text.Json.JsonSerializer.Serialize(recorded.Details), StringComparison.Ordinal);
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
        var recorded = Assert.Single(_events.Recorded);
        Assert.Equal((SecurityEventType.SignedIn, userId, session.Id), (recorded.Type, recorded.TargetUserId, recorded.SessionId));
    }

    [Fact]
    public async Task Reusing_a_refresh_token_saves_the_revocation_and_warns()
    {
        var (session, refresh) = StartApiSession();
        var log = new ListLogger();
        var handler = Refresh(log);
        Assert.True((await handler.HandleAsync(new RefreshApiTokenCommand(refresh), default)).IsSuccess);

        var reused = await handler.HandleAsync(new RefreshApiTokenCommand(refresh), default);

        Assert.Same(SessionDomainErrors.RefreshTokenReused, reused.Error);
        Assert.True(session.IsRevoked);
        Assert.Equal(2, _persistence.Saves);
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Warning);
        var recorded = Assert.Single(_events.Recorded);
        Assert.Equal((SecurityEventType.RefreshTokenReused, session.Id), (recorded.Type, recorded.SessionId));
    }

    [Fact]
    public async Task A_refresh_token_from_several_rotations_ago_still_ends_the_session()
    {
        var (session, first) = StartApiSession();
        var handler = Refresh(new ListLogger());
        var second = (await handler.HandleAsync(new RefreshApiTokenCommand(first), default)).Value.RefreshToken;
        Assert.True((await handler.HandleAsync(new RefreshApiTokenCommand(second), default)).IsSuccess); // an attacker rotated twice

        var reused = await handler.HandleAsync(new RefreshApiTokenCommand(first), default);

        Assert.Same(SessionDomainErrors.RefreshTokenReused, reused.Error);
        Assert.Equal(SessionRevokeReason.RefreshTokenReused, session.RevokedReason);
        Assert.Equal(2, session.RetiredRefreshTokens.Count);
        Assert.Equal(SecurityEventType.RefreshTokenReused, Assert.Single(_events.Recorded).Type);
    }

    [Fact]
    public async Task Two_refreshes_racing_with_one_token_end_the_session()
    {
        var (session, refresh) = StartApiSession();
        _persistence.SaveResults.Enqueue(CommonErrors.ConcurrencyConflict); // the other request retired the token first

        var raced = await Refresh(new ListLogger()).HandleAsync(new RefreshApiTokenCommand(refresh), default);

        Assert.Same(SessionDomainErrors.RefreshTokenReused, raced.Error);
        Assert.Equal(1, _persistence.Discards);
        Assert.Equal(SessionRevokeReason.RefreshTokenReused, session.RevokedReason);
        Assert.Equal(2, _persistence.Saves);
    }

    [Fact]
    public async Task A_deactivated_users_session_ends_instead_of_rotating()
    {
        var (session, refresh) = StartApiSession();
        _accounts.Users[session.UserId] = _accounts.Users[session.UserId] with { IsActive = false };

        var result = await Refresh(new ListLogger()).HandleAsync(new RefreshApiTokenCommand(refresh), default);

        Assert.Same(SessionDomainErrors.Revoked, result.Error);
        Assert.Equal(SessionRevokeReason.UserDeactivated, session.RevokedReason);
        Assert.Empty(session.RetiredRefreshTokens);
        var recorded = Assert.Single(_events.Recorded);
        Assert.Equal((SecurityEventType.SessionRevoked, session.Id), (recorded.Type, recorded.SessionId));
    }

    [Fact]
    public async Task A_deactivated_users_ended_session_answers_revoked_without_another_event()
    {
        var (session, refresh) = StartApiSession();
        _accounts.Users[session.UserId] = _accounts.Users[session.UserId] with { IsActive = false };
        session.Revoke(SessionRevokeReason.UserDeactivated, _clock.GetUtcNow().UtcDateTime); // by the deactivation itself

        var result = await Refresh(new ListLogger()).HandleAsync(new RefreshApiTokenCommand(refresh), default);

        Assert.Same(SessionDomainErrors.Revoked, result.Error);
        Assert.Empty(_events.Recorded);
        Assert.Equal(0, _persistence.Saves);
    }

    [Fact]
    public async Task An_old_refresh_token_of_an_expired_session_only_answers_revoked()
    {
        var (session, first) = StartApiSession();
        var handler = Refresh(new ListLogger());
        var second = (await handler.HandleAsync(new RefreshApiTokenCommand(first), default)).Value.RefreshToken;
        Assert.True((await handler.HandleAsync(new RefreshApiTokenCommand(second), default)).IsSuccess);
        _clock.Advance(TimeSpan.FromDays(31)); // past the session's absolute lifetime
        var log = new ListLogger();

        var replayed = await Refresh(log).HandleAsync(new RefreshApiTokenCommand(first), default);

        Assert.Same(SessionDomainErrors.Revoked, replayed.Error);
        Assert.False(session.IsRevoked);
        Assert.Empty(_events.Recorded);
        Assert.DoesNotContain(log.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task A_detected_reuse_ends_the_session_even_when_the_client_has_left()
    {
        var (session, first) = StartApiSession();
        Assert.True((await Refresh(new ListLogger()).HandleAsync(new RefreshApiTokenCommand(first), default)).IsSuccess);
        using var left = new CancellationTokenSource();
        await left.CancelAsync();

        var reused = await Refresh(new ListLogger()).HandleAsync(new RefreshApiTokenCommand(first), left.Token);

        Assert.Same(SessionDomainErrors.RefreshTokenReused, reused.Error);
        Assert.True(session.IsRevoked);
        Assert.False(_persistence.SaveTokens[^1].IsCancellationRequested);
    }

    [Fact]
    public async Task An_old_refresh_token_of_an_ended_session_only_answers_revoked()
    {
        var (session, first) = StartApiSession();
        var handler = Refresh(new ListLogger());
        var second = (await handler.HandleAsync(new RefreshApiTokenCommand(first), default)).Value.RefreshToken;
        Assert.True((await handler.HandleAsync(new RefreshApiTokenCommand(second), default)).IsSuccess);
        session.Revoke(SessionRevokeReason.Logout, _clock.GetUtcNow().UtcDateTime);
        var log = new ListLogger();

        var replayed = await Refresh(log).HandleAsync(new RefreshApiTokenCommand(first), default);

        Assert.Same(SessionDomainErrors.Revoked, replayed.Error);
        Assert.Equal(SessionRevokeReason.Logout, session.RevokedReason);
        Assert.Empty(_events.Recorded);
        Assert.DoesNotContain(log.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task The_attempt_that_starts_a_lockout_is_recorded()
    {
        var userId = Guid.NewGuid();
        _accounts.NextLogin = new LoginCheck(LoginOutcome.WrongPassword, userId, LockoutStarted: true);

        await Login(new ListLogger()).HandleAsync(new LoginCommand("u", "p"), default);

        Assert.Equal([SecurityEventType.SignInFailed, SecurityEventType.UserLockedOut], _events.Types);
        Assert.All(_events.Recorded, e => Assert.Equal(userId, e.TargetUserId));
    }

    [Fact]
    public async Task A_counted_attempt_is_recorded_even_when_the_client_has_left()
    {
        _accounts.NextLogin = new LoginCheck(LoginOutcome.WrongPassword, Guid.NewGuid());
        using var left = new CancellationTokenSource();
        await left.CancelAsync();

        var result = await Login(new ListLogger()).HandleAsync(new LoginCommand("u", "p"), left.Token);

        Assert.Same(AuthenticationErrors.LoginFailed, result.Error);
        Assert.False(Assert.Single(_persistence.SaveTokens).IsCancellationRequested);
        Assert.Equal([SecurityEventType.SignInFailed], _events.Types);
    }

    [Fact]
    public async Task Wrong_current_password_counts_toward_lockout_and_changes_nothing()
    {
        var caller = new FakeCaller();
        _sessions.StartWeb(caller.UserId!.Value, _clock.GetUtcNow().UtcDateTime);
        _accounts.NextPasswordChange = new(AuthenticationErrors.PasswordIncorrect);

        var result = await ChangePassword(caller).HandleAsync(new ChangeMyPasswordCommand("wrong", "New-Password-1", Guid.NewGuid()), default);

        Assert.Same(AuthenticationErrors.PasswordIncorrect, result.Error);
        Assert.Equal(1, _persistence.Saves);
        Assert.DoesNotContain(_sessions.All, s => s.IsRevoked);
        Assert.Equal([SecurityEventType.PasswordChangeRejected], _events.Types);
    }

    [Fact]
    public async Task A_wrong_current_password_that_starts_a_lockout_is_recorded_even_when_the_client_has_left()
    {
        var caller = new FakeCaller();
        _accounts.NextPasswordChange = new(AuthenticationErrors.PasswordIncorrect, LockoutStarted: true);
        using var left = new CancellationTokenSource();
        await left.CancelAsync();

        var result = await ChangePassword(caller).HandleAsync(new ChangeMyPasswordCommand("wrong", "New-Password-1", Guid.NewGuid()), left.Token);

        Assert.Same(AuthenticationErrors.PasswordIncorrect, result.Error);
        Assert.Equal([SecurityEventType.PasswordChangeRejected, SecurityEventType.UserLockedOut], _events.Types);
        Assert.False(Assert.Single(_persistence.SaveTokens).IsCancellationRequested);
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
        Assert.Equal([SecurityEventType.PrivilegeEscalationBlocked, SecurityEventType.PrivilegeEscalationBlocked], _events.Types);
        Assert.Equal(2, _persistence.Saves); // the two refusals; the unknown role is a plain validation failure
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
    public async Task Removing_one_of_two_administrators_versions_the_role_so_a_parallel_removal_conflicts()
    {
        var otherAdmin = _accounts.Add(_roles.AdministratorId);
        _accounts.Add(_roles.AdministratorId);
        var reader = _roles.Add("Reader", permissions: WarehousesRead);

        var touchedBeforeCounting = false;
        _accounts.CountingStarted = () => touchedBeforeCounting = _roles.Touched.Contains(_roles.AdministratorId);

        var result = await SetRoles(new FakeCaller()).HandleAsync(new SetUserRolesCommand(otherAdmin, [reader]), default);

        Assert.True(result.IsSuccess);
        Assert.Equal([_roles.AdministratorId], _roles.Touched);
        Assert.True(touchedBeforeCounting); // read after the count, a removal committing in between would go unseen
    }

    [Fact]
    public async Task The_last_company_wide_administrator_keeps_the_role_the_account_and_the_scope()
    {
        var (branch, _) = _organization.AddWarehouse();
        var companyWide = _accounts.Add(_roles.AdministratorId);
        _access.Add(UserAccess.Create(companyWide, companyWide: true, [], []));
        var branchAdmin = _accounts.Add(_roles.AdministratorId); // another Administrator, but only for one branch
        _access.Add(UserAccess.Create(branchAdmin, companyWide: false, [branch], []));
        var reader = _roles.Add("Reader", permissions: WarehousesRead);
        var caller = new FakeCaller();

        var reroled = await SetRoles(caller).HandleAsync(new SetUserRolesCommand(companyWide, [reader]), default);
        var deactivated = await SetActive(caller).HandleAsync(new SetUserActiveCommand(companyWide, false), default);
        var narrowed = await SetAccess(caller).HandleAsync(new SetUserAccessCommand(companyWide, false, [branch], [], 0), default);

        Assert.All([reroled.Error, deactivated.Error, narrowed.Error], e => Assert.Same(UserErrors.LastAdministrator, e));
        Assert.Empty(_accounts.Changes);
        Assert.True(_access.ByUser[companyWide].IsCompanyWide);

        // With a second company-wide Administrator, the first may become branch-scoped.
        _access.Add(UserAccess.Create(_accounts.Add(_roles.AdministratorId), companyWide: true, [], []));
        var allowed = await SetAccess(caller).HandleAsync(new SetUserAccessCommand(companyWide, false, [branch], [], 0), default);
        Assert.True(allowed.IsSuccess);
        Assert.False(_access.ByUser[companyWide].IsCompanyWide);
    }

    [Fact]
    public async Task Nobody_unlocks_their_own_account()
    {
        var caller = new FakeCaller();

        var result = await Unlock(caller).HandleAsync(new UnlockUserCommand(caller.UserId!.Value), default);

        Assert.Same(UserErrors.CannotUnlockSelf, result.Error);
        Assert.Empty(_accounts.Changes);
    }

    [Fact]
    public async Task A_branch_manager_cannot_take_over_a_company_wide_account()
    {
        var (branch, _) = _organization.AddWarehouse();
        var caller = new FakeCaller([IdentityPermissions.UsersManage, IdentityPermissions.AccessManage, WarehousesRead],
            allWarehouses: false);
        _access.Add(UserAccess.Create(caller.UserId!.Value, companyWide: false, [branch], []));
        var reader = _roles.Add("Reader", permissions: WarehousesRead);
        var target = _accounts.Add(reader); // holds nothing the caller lacks, but reaches every warehouse
        _access.Add(UserAccess.Create(target, companyWide: true, [], []));

        var reset = await ResetPassword(caller).HandleAsync(new ResetUserPasswordCommand(target, "Taken-over-123"), default);
        var reroled = await SetRoles(caller).HandleAsync(new SetUserRolesCommand(target, [reader]), default);
        var narrowed = await SetAccess(caller).HandleAsync(new SetUserAccessCommand(target, false, [branch], [], 0), default);

        Assert.All([reset.Error, reroled.Error, narrowed.Error], e => Assert.Same(UserAccessErrors.ExceedsOwnScope, e));
        Assert.Empty(_accounts.Changes);
        Assert.True(_access.ByUser[target].IsCompanyWide);
        Assert.Equal(3, _events.Recorded.Count(e => e.Type == SecurityEventType.PrivilegeEscalationBlocked && e.TargetUserId == target));
    }

    [Fact]
    public async Task Nobody_resets_their_own_password_without_the_current_one()
    {
        var caller = new FakeCaller();
        _accounts.Users[caller.UserId!.Value] = new UserAccount(caller.UserId.Value, "me", "Me", null, IsActive: true, IsLockedOut: false);

        var result = await ResetPassword(caller).HandleAsync(new ResetUserPasswordCommand(caller.UserId.Value, "New-Password-1"), default);

        Assert.Same(UserErrors.CannotResetOwnPassword, result.Error);
        Assert.Empty(_accounts.Changes);
    }

    [Fact]
    public async Task Only_a_holder_of_every_permission_grants_administrator()
    {
        var everything = Catalog.Select(p => p.Name).ToList(); // today's catalog, but not "*"
        var caller = new FakeCaller(everything);
        var user = _accounts.Add();

        var result = await SetRoles(caller).HandleAsync(new SetUserRolesCommand(user, [_roles.AdministratorId]), default);

        Assert.Same(PermissionErrors.ExceedsOwnPermissions, result.Error);
    }

    [Fact]
    public async Task Nobody_strips_a_permission_they_lack_from_a_role()
    {
        var roleId = Guid.NewGuid();
        _roles.Roles[roleId] = Role.Restore(roleId, "Clerk", null, isSystem: false, [WarehousesRead, ProductsRead], version: "v1");
        var caller = new FakeCaller([IdentityPermissions.RolesManage, WarehousesRead]);
        var handler = new UpdateRoleHandler(caller, _roles, Catalog, _persistence, NullLogger<UpdateRoleHandler>.Instance, _events);

        var result = await handler.HandleAsync(new UpdateRoleCommand(roleId, "Clerk", null, [WarehousesRead], "v1"), default);

        Assert.Same(PermissionErrors.ExceedsOwnPermissions, result.Error);
        Assert.Equal([SecurityEventType.PrivilegeEscalationBlocked], _events.Types);
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

        Assert.Same(UserAccessErrors.ExceedsOwnScope, (await handler.HandleAsync(new(user, true, [], [], 0), default)).Error);
        Assert.Same(UserAccessErrors.ExceedsOwnScope, (await handler.HandleAsync(new(user, false, [], [outside], 0), default)).Error);
        Assert.Same(UserAccessErrors.ExceedsOwnScope, (await handler.HandleAsync(new(user, false, [otherBranch], [], 0), default)).Error);
        Assert.Same(UserAccessErrors.UnknownWarehouses, (await handler.HandleAsync(new(user, false, [], [Guid.NewGuid()], 0), default)).Error);
        Assert.Equal(3, _persistence.Saves); // only the three refused attempts, recorded
        Assert.Equal(3, _events.Recorded.Count(e => e.Type == SecurityEventType.PrivilegeEscalationBlocked && e.TargetUserId == user));
        Assert.False(_access.ByUser.ContainsKey(user));

        Assert.True((await handler.HandleAsync(new(user, false, [branch], [inBranch], 0), default)).IsSuccess);
        Assert.True((await handler.HandleAsync(new(user, false, [branch], [], 0), default)).IsSuccess);
        Assert.Equal(1, _access.ByUser[user].Revision); // created, then changed once
    }

    [Fact]
    public async Task A_scope_edit_from_a_stale_screen_is_a_conflict_and_changes_nothing()
    {
        var (branch, _) = _organization.AddWarehouse();
        var user = _accounts.Add();
        _access.Add(UserAccess.Create(user, companyWide: false, [branch], []));
        var handler = SetAccess(new FakeCaller());
        Assert.True((await handler.HandleAsync(new(user, true, [], [], 0), default)).IsSuccess); // revision 0 -> 1

        var stale = await handler.HandleAsync(new(user, false, [], [], 0), default);

        Assert.Same(CommonErrors.ConcurrencyConflict, stale.Error);
        Assert.True(_access.ByUser[user].IsCompanyWide);
        Assert.Equal(1, _persistence.Saves);
        Assert.Equal([SecurityEventType.UserAccessChanged], _events.Types);
    }

    [Fact]
    public async Task Creating_a_user_with_scope_needs_access_manage()
    {
        var caller = new FakeCaller([IdentityPermissions.UsersManage]);
        var handler = new CreateUserHandler(caller, _accounts, _accounts, _roles, _access, _organization, _organization, Catalog, _persistence,
            NullLogger<CreateUserHandler>.Instance, _events);

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
        var handler = new CreateRoleHandler(caller, _roles, Catalog, _persistence, NullLogger<CreateRoleHandler>.Instance, _events);

        var result = await handler.HandleAsync(new CreateRoleCommand("Clerk", null, [WarehousesRead, ProductsRead]), default);

        Assert.Same(PermissionErrors.ExceedsOwnPermissions, result.Error);
        Assert.Empty(_roles.Added);
        Assert.Equal([SecurityEventType.PrivilegeEscalationBlocked], _events.Types);
    }

    [Fact]
    public async Task A_role_edit_from_a_stale_version_is_a_conflict()
    {
        var roleId = Guid.NewGuid();
        _roles.Roles[roleId] = Role.Restore(roleId, "Clerk", null, isSystem: false, [WarehousesRead], version: "v2");
        var handler = new UpdateRoleHandler(new FakeCaller(), _roles, Catalog, _persistence, NullLogger<UpdateRoleHandler>.Instance, _events);

        var stale = await handler.HandleAsync(new UpdateRoleCommand(roleId, "Clerk", null, [ProductsRead], "v1"), default);
        var current = await handler.HandleAsync(new UpdateRoleCommand(roleId, "Clerk", null, [ProductsRead], "v2"), default);

        Assert.Same(CommonErrors.ConcurrencyConflict, stale.Error);
        Assert.True(current.IsSuccess);
        Assert.Equal(1, _persistence.Saves);
        var recorded = Assert.Single(_events.Recorded);
        Assert.Equal((SecurityEventType.RoleUpdated, roleId), (recorded.Type, recorded.RoleId));
    }

    [Fact]
    public async Task Another_users_session_does_not_exist_for_a_caller_without_sessions_manage()
    {
        var caller = new FakeCaller([WarehousesRead]);
        var mine = _sessions.StartWeb(caller.UserId!.Value, _clock.GetUtcNow().UtcDateTime);
        var theirs = _sessions.StartWeb(Guid.NewGuid(), _clock.GetUtcNow().UtcDateTime);
        var handler = new RevokeSessionHandler(caller, _sessions, _persistence, _clock, NullLogger<RevokeSessionHandler>.Instance, _events);

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

    private (UserSession Session, string RefreshToken) StartApiSession()
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var (refresh, refreshHash) = SessionTokens.Create();
        var session = UserSession.StartApi(_accounts.Add(), null,
            new IssuedApiTokens(SessionTokens.Create().Hash, now.AddMinutes(15), refreshHash, now.AddDays(14)), now, TimeSpan.FromDays(30));
        _sessions.Add(session);
        return (session, refresh);
    }

    private RefreshApiTokenHandler Refresh(ListLogger log) =>
        new(_sessions, _accounts, _persistence, Sessions, _clock, new CapturingLogger<RefreshApiTokenHandler>(log), _events);

    private LoginHandler Login(ListLogger log) =>
        new(_accounts, _sessions, _persistence, Sessions, _clock, new CapturingLogger<LoginHandler>(log), _events);

    private ChangeMyPasswordHandler ChangePassword(ICurrentUser caller) =>
        new(caller, _accounts, _sessions, _persistence, _clock, NullLogger<ChangeMyPasswordHandler>.Instance, _events);

    private SetUserActiveHandler SetActive(ICurrentUser caller) =>
        new(caller, _accounts, _accounts, _roles, _access, _sessions, Catalog, _persistence, _clock, NullLogger<SetUserActiveHandler>.Instance, _events);

    private ResetUserPasswordHandler ResetPassword(ICurrentUser caller) =>
        new(caller, _accounts, _accounts, _roles, _access, _sessions, Catalog, _persistence, _clock, NullLogger<ResetUserPasswordHandler>.Instance, _events);

    private SetUserRolesHandler SetRoles(ICurrentUser caller) =>
        new(caller, _accounts, _accounts, _roles, _access, Catalog, _persistence, NullLogger<SetUserRolesHandler>.Instance, _events);

    private SetUserAccessHandler SetAccess(ICurrentUser caller) =>
        new(caller, _accounts, _accounts, _roles, _access, _organization, _organization, Catalog, _persistence, NullLogger<SetUserAccessHandler>.Instance, _events);

    private UnlockUserHandler Unlock(ICurrentUser caller) =>
        new(caller, _accounts, _accounts, _roles, _access, Catalog, _persistence, _events);

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
            ? new UserDetailsResponse(id, "me", "Me", null, true, false, [], new UserAccessResponse(false, [], [], 0))
            : null);

        public Task<WarehouseERP.BuildingBlocks.Application.Pagination.PagedResult<UserSummaryResponse>> ListAsync(
            string? search, int page, int pageSize, CancellationToken ct) => throw new NotSupportedException();
    }
}
