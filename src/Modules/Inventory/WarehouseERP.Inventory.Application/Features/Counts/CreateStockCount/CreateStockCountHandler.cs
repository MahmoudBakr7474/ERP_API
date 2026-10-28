using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Counts.CreateStockCount;

// A draft count of a physical warehouse in the user's scope, whole or some of its active locations. Nothing is frozen
// until it starts.
public sealed partial class CreateStockCountHandler(
    IStockCountRepository counts, StockReferences references, ICurrentUser currentUser, TimeProvider clock,
    ILogger<CreateStockCountHandler> logger)
    : ICommandHandler<CreateStockCountCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateStockCountCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return StockErrors.WarehouseAccessDenied;
        var warehouse = await references.UsableWarehouseAsync(command.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        var locationIds = command.LocationIds ?? [];
        var locations = await references.WarehouseLocationsAsync(command.WarehouseId, locationIds, ct);
        if (locations.IsFailure)
            return locations.Error;

        var created = StockCount.Create(
            command.WarehouseId, warehouse.Value.BranchId, locationIds, command.Notes, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        counts.Add(created.Value);
        var saved = await counts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        CountCreated(logger, created.Value.Id);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 5030, Level = LogLevel.Information, Message = "Stock count {CountId} created")]
    private static partial void CountCreated(ILogger logger, Guid countId);
}
