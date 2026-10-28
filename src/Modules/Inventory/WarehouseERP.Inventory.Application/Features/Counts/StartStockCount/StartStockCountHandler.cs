using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Counts.StartStockCount;

// Starting a count takes the warehouse's lock exclusively, so no posting is in flight while the freeze is persisted and
// the expected quantities are snapshotted; the transaction then ends and nothing is held while people count. From the
// commit on, every posting sees the active scope after taking its own lock and stays out.
public sealed partial class StartStockCountHandler(
    ITransactionRunner transactions, IInventoryLocks locks, IStockCountRepository counts, StockReferences references,
    ICurrentUser currentUser, TimeProvider clock, ILogger<StartStockCountHandler> logger)
    : ICommandHandler<StartStockCountCommand, string>
{
    public async Task<Result<string>> HandleAsync(StartStockCountCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await CountAccess.AuthorizeAsync(counts, currentUser, command.CountId, ct);
        if (access.IsFailure)
            return access.Error;
        var started = await transactions.RunAsync(token => StartAsync(command.CountId, expected, token), ct);
        if (started.IsSuccess)
            CountStarted(logger, command.CountId);
        return started;
    }

    private async Task<Result<string>> StartAsync(Guid countId, byte[] expected, CancellationToken ct)
    {
        var count = await counts.GetForUpdateAsync(countId, expected, ct);
        if (count is null)
            return StockCountErrors.NotFound;
        if (!await locks.TryLockWarehousesAsync([count.WarehouseId], exclusive: true, ct))
            return StockErrors.Busy;
        var warehouse = await references.UsableWarehouseAsync(count.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        var listed = count.Scopes.Where(s => s.LocationId.HasValue).Select(s => s.LocationId!.Value).ToList();
        var locations = await references.WarehouseLocationsAsync(count.WarehouseId, listed, ct);
        if (locations.IsFailure)
            return locations.Error;
        if (await counts.OverlapsActiveScopeAsync(count, ct))
            return StockCountErrors.ScopeOverlaps;

        var snapshot = await counts.ReadScopeBalancesAsync(count, ct);
        var begun = count.Start(snapshot, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (begun.IsFailure)
            return begun.Error;
        var saved = await counts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        return Convert.ToBase64String(count.RowVersion);
    }

    [LoggerMessage(EventId = 5031, Level = LogLevel.Information, Message = "Stock count {CountId} started: scope frozen")]
    private static partial void CountStarted(ILogger logger, Guid countId);
}
