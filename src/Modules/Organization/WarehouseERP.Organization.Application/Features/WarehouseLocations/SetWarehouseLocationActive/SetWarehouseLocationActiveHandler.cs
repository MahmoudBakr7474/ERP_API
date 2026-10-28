using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.SetWarehouseLocationActive;

// The domain keeps the default location active. Deactivating asks Inventory in the same transaction, under the
// warehouse's inventory lock, so no posting can put stock in the location in between.
public sealed partial class SetWarehouseLocationActiveHandler(
    IWarehouseRepository warehouses, ICurrentUser currentUser, ITransactionRunner transactions, IWarehouseStockGuard stock,
    ILogger<SetWarehouseLocationActiveHandler> logger)
    : ICommandHandler<SetWarehouseLocationActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetWarehouseLocationActiveCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return WarehouseErrors.AccessDenied;

        var changed = await transactions.RunAsync(token => ChangeAsync(command, token), ct);
        if (changed.IsFailure)
            return changed.Error;

        ActiveChanged(logger, command.LocationId, command.IsActive);
        return Done.Value;
    }

    private async Task<Result<Done>> ChangeAsync(SetWarehouseLocationActiveCommand command, CancellationToken ct)
    {
        var location = await warehouses.GetLocationForUpdateAsync(command.WarehouseId, command.LocationId, expectedRowVersion: null, ct);
        if (location is null)
            return WarehouseLocationErrors.NotFound;

        var changed = location.SetActive(command.IsActive);
        if (changed.IsFailure)
            return changed.Error;
        if (!command.IsActive)
        {
            var occupancy = await stock.LockAndCheckLocationAsync(command.WarehouseId, location.Id, ct);
            if (occupancy != StockOccupancy.Free)
                return occupancy switch
                {
                    StockOccupancy.HoldsStock => WarehouseLocationErrors.HoldsStock,
                    StockOccupancy.HasOpenDocuments => WarehouseLocationErrors.HasOpenInventoryDocuments,
                    _ => WarehouseErrors.InventoryBusy,
                };
        }

        var saved = await warehouses.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        return Done.Value;
    }

    [LoggerMessage(EventId = 2011, Level = LogLevel.Information, Message = "Location {LocationId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid locationId, bool isActive);
}
