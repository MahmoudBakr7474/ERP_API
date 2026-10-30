using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WarehouseERP.Accounting.Application;
using WarehouseERP.Accounting.Application.Features.SourcePostings;

namespace WarehouseERP.Accounting.Infrastructure;

/// <summary>
/// Journals operational documents every <c>Accounting:Integration:Interval</c> once accounting is active: finds new
/// documents and posts what is due (SourceSync), in a scope of its own per run. A failed run is logged and the next tick
/// tries again; nothing is lost on a restart, because the documents themselves and their accounting identities are the
/// queue. Public so tests can run one pass.
/// </summary>
public sealed partial class SourcePostingWorker(
    IServiceScopeFactory scopes, IOptions<AccountingIntegrationOptions> options, TimeProvider clock, ILogger<SourcePostingWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.WorkerEnabled)
            return;
        using var timer = new PeriodicTimer(options.Value.Interval, clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                RunFailed(logger, ex); // logged once; the next tick tries again
            }
        }
    }

    /// <summary>One run; does nothing until accounting is active.</summary>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SourceSync>().RunAsync(fullScan: false, ct);
    }

    [LoggerMessage(EventId = 9230, Level = LogLevel.Error, Message = "The accounting run failed")]
    private static partial void RunFailed(ILogger logger, Exception exception);
}
