using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WarehouseERP.Identity.Application;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

/// <summary>
/// Deletes session rows that ended (expired or revoked) longer than <c>Identity:Sessions:RetentionAfterEnd</c> ago,
/// every <c>CleanupInterval</c>. The security audit trail keeps the history of sign-ins and revocations, so nothing is
/// lost; without this the sessions table grows forever. Public so tests can run one pass.
/// </summary>
public sealed partial class ExpiredSessionCleanup(
    IServiceScopeFactory scopes, IOptions<IdentitySessionOptions> options, TimeProvider clock, ILogger<ExpiredSessionCleanup> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.CleanupInterval, clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CleanUpAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                CleanupFailed(logger, ex); // logged once; the next tick tries again
            }
        }
    }

    /// <summary>One pass; returns the number of rows deleted.</summary>
    public async Task<int> CleanUpAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();
        var cutoff = clock.GetUtcNow().UtcDateTime - options.Value.RetentionAfterEnd;
        var deleted = await db.Set<UserSession>()
            .Where(s => s.ExpiresAtUtc < cutoff || s.RevokedAtUtc < cutoff)
            .ExecuteDeleteAsync(ct);
        if (deleted > 0)
            SessionsDeleted(logger, deleted, cutoff);
        return deleted;
    }

    [LoggerMessage(EventId = 4050, Level = LogLevel.Information, Message = "Deleted {Count} session(s) that ended before {CutoffUtc}")]
    private static partial void SessionsDeleted(ILogger logger, int count, DateTime cutoffUtc);

    [LoggerMessage(EventId = 4051, Level = LogLevel.Error, Message = "Expired-session cleanup failed")]
    private static partial void CleanupFailed(ILogger logger, Exception exception);
}
