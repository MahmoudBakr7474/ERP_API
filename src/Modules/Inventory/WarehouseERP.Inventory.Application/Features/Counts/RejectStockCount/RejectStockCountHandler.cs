using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Counts.RejectStockCount;

// An approver sends a submitted count back for counting with a reason; the scope stays frozen.
public sealed partial class RejectStockCountHandler(
    IStockCountRepository counts, ICurrentUser currentUser, TimeProvider clock, ILogger<RejectStockCountHandler> logger)
    : ICommandHandler<RejectStockCountCommand, string>
{
    public async Task<Result<string>> HandleAsync(RejectStockCountCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await CountAccess.AuthorizeAsync(counts, currentUser, command.CountId, ct);
        if (access.IsFailure)
            return access.Error;
        var count = await counts.GetForUpdateAsync(command.CountId, expected, ct);
        if (count is null)
            return StockCountErrors.NotFound;

        var rejected = count.Reject(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (rejected.IsFailure)
            return rejected.Error;
        var saved = await counts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        CountRejected(logger, count.Id);
        return Convert.ToBase64String(count.RowVersion);
    }

    [LoggerMessage(EventId = 5034, Level = LogLevel.Information, Message = "Stock count {CountId} rejected")]
    private static partial void CountRejected(ILogger logger, Guid countId);
}
