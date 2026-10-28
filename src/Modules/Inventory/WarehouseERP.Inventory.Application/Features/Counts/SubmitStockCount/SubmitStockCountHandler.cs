using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Counts.SubmitStockCount;

// Every line counted, the count waits for an approver; the scope stays frozen. Returns the new row version.
public sealed partial class SubmitStockCountHandler(
    IStockCountRepository counts, ICurrentUser currentUser, TimeProvider clock, ILogger<SubmitStockCountHandler> logger)
    : ICommandHandler<SubmitStockCountCommand, string>
{
    public async Task<Result<string>> HandleAsync(SubmitStockCountCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await CountAccess.AuthorizeAsync(counts, currentUser, command.CountId, ct);
        if (access.IsFailure)
            return access.Error;
        var count = await counts.GetForUpdateAsync(command.CountId, expected, ct);
        if (count is null)
            return StockCountErrors.NotFound;

        var submitted = count.Submit(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (submitted.IsFailure)
            return submitted.Error;
        var saved = await counts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        CountSubmitted(logger, count.Id);
        return Convert.ToBase64String(count.RowVersion);
    }

    [LoggerMessage(EventId = 5033, Level = LogLevel.Information, Message = "Stock count {CountId} submitted for approval")]
    private static partial void CountSubmitted(ILogger logger, Guid countId);
}
