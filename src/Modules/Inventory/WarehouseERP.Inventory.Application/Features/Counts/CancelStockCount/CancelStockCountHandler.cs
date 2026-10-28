using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Counts.CancelStockCount;

// Abandoning a count releases its freeze; nothing posts. Returns the new row version.
public sealed partial class CancelStockCountHandler(
    IStockCountRepository counts, ICurrentUser currentUser, TimeProvider clock, ILogger<CancelStockCountHandler> logger)
    : ICommandHandler<CancelStockCountCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelStockCountCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await CountAccess.AuthorizeAsync(counts, currentUser, command.CountId, ct);
        if (access.IsFailure)
            return access.Error;
        var count = await counts.GetForUpdateAsync(command.CountId, expected, ct);
        if (count is null)
            return StockCountErrors.NotFound;

        var cancelled = count.Cancel(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (cancelled.IsFailure)
            return cancelled.Error;
        var saved = await counts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        CountCancelled(logger, count.Id);
        return Convert.ToBase64String(count.RowVersion);
    }

    [LoggerMessage(EventId = 5035, Level = LogLevel.Information, Message = "Stock count {CountId} cancelled: freeze released")]
    private static partial void CountCancelled(ILogger logger, Guid countId);
}
