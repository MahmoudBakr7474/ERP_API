using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure;

// With no setting, or once any user exists, this does nothing (and touches no database).
internal sealed partial class IdentityBootstrapper(
    IServiceScopeFactory scopes, IOptions<IdentityBootstrapOptions> options, ILogger<IdentityBootstrapper> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
            return;

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();
        if (await db.Set<ApplicationUser>().AnyAsync(cancellationToken))
        {
            BootstrapIgnored(logger);
            return;
        }

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var administrator = new ApplicationUser { UserName = settings.UserName!.Trim(), DisplayName = settings.DisplayName };
        var created = await users.CreateAsync(administrator, settings.Password!);
        if (!created.Succeeded) // e.g. the password breaks Identity:Password; codes only, never the value
            throw new InvalidOperationException(
                "Identity:Bootstrap could not create the first administrator: " + string.Join(", ", created.Errors.Select(e => e.Code)));
        db.Set<IdentityUserRole<Guid>>().Add(new IdentityUserRole<Guid> { UserId = administrator.Id, RoleId = ApplicationRole.AdministratorId });
        db.Set<UserAccess>().Add(UserAccess.Create(administrator.Id, companyWide: true, [], []));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbFailures.UniqueConstraintName(ex) == "UserNameIndex")
        {
            BootstrapIgnored(logger); // another instance starting at the same time created it
            return;
        }
        AdministratorCreated(logger, administrator.Id);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 4040, Level = LogLevel.Warning,
        Message = "First administrator {UserId} created from Identity:Bootstrap; remove the setting now")]
    private static partial void AdministratorCreated(ILogger logger, Guid userId);

    [LoggerMessage(EventId = 4041, Level = LogLevel.Information,
        Message = "Identity:Bootstrap is set but users already exist; it was ignored and can be removed")]
    private static partial void BootstrapIgnored(ILogger logger);
}
