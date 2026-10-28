using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Persistence;

// ADR 0004's transaction port over the shared DbContext. Each attempt runs inside EF Core's execution strategy (as EF
// requires for user transactions) and starts from an empty change tracker, so a retry re-reads everything. Only
// deadlocks and transient connection failures are retried; a commit that fails that way may still have committed,
// which is why the work checks its own durable record first (ITransactionRunner remarks).
public sealed partial class EfTransactionRunner(DbContext db, ILogger<EfTransactionRunner> logger) : ITransactionRunner
{
    private const int MaxAttempts = 3;

    // Deadlock victim, lock request timeout, timeout, and the connection-level failures SQL Server reports as transient.
    private static readonly HashSet<int> TransientErrors = [1205, 1222, -2, 233, 64, 121, 10053, 10054, 10060, 4060, 40197, 40501, 40613, 49918, 49919, 49920];

    public async Task<Result<T>> RunAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await strategy.ExecuteAsync(token => RunOnceAsync(work, token), ct);
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsTransient(ex))
            {
                Retrying(logger, attempt, ex.GetType().Name);
                db.ChangeTracker.Clear();
            }
        }
    }

    private async Task<Result<T>> RunOnceAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var result = await work(ct);
        if (result.IsFailure)
        {
            await transaction.RollbackAsync(ct);
            // Nothing the failed work changed may be saved later by accident.
            db.ChangeTracker.Clear();
            return result;
        }

        await transaction.CommitAsync(ct);
        return result;
    }

    private static bool IsTransient(Exception exception) => exception switch
    {
        TransientDatabaseException => true,
        SqlException sql => sql.Errors.Cast<SqlError>().Any(e => TransientErrors.Contains(e.Number)),
        DbUpdateException { InnerException: { } inner } => IsTransient(inner),
        InvalidOperationException { InnerException: { } inner } => IsTransient(inner),
        _ => false,
    };

    [LoggerMessage(EventId = 9101, Level = LogLevel.Warning, Message = "Transaction attempt {Attempt} failed with {Exception}; running it again")]
    private static partial void Retrying(ILogger logger, int attempt, string exception);
}
