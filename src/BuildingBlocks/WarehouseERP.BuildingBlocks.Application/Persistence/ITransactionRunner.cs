using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.BuildingBlocks.Application.Persistence;

/// <summary>
/// One explicit database transaction around a use case that must change several things atomically (a stock posting,
/// a deactivation that waits for Inventory's lock). Commits when the work succeeds, rolls back when it fails.
/// </summary>
/// <remarks>
/// A deadlock or a transient failure, including one while committing, runs the whole work again a bounded number of
/// times. The work must therefore read everything it needs inside the call, and a work whose earlier attempt may have
/// committed must find out from its own durable record (an idempotency key) before changing anything again.
/// </remarks>
public interface ITransactionRunner
{
    Task<Result<T>> RunAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken ct);
}
