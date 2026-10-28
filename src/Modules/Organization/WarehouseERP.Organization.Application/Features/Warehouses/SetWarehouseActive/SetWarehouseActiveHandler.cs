using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Warehouses.SetWarehouseActive;

// Scope before existence. Reactivating needs an active branch, so an inactive branch never holds an active
// warehouse (SetBranchActive refuses the other direction). Deactivating asks Inventory in the same transaction, which
// holds Inventory's warehouse lock until the deactivation commits, so no posting can put stock there in between.
public sealed partial class SetWarehouseActiveHandler(
    IWarehouseRepository warehouses, ICurrentUser currentUser, ITransactionRunner transactions, IWarehouseStockGuard stock,
    ILogger<SetWarehouseActiveHandler> logger)
    : ICommandHandler<SetWarehouseActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetWarehouseActiveCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return WarehouseErrors.AccessDenied;

        var changed = await transactions.RunAsync(token => ChangeAsync(command, token), ct);
        if (changed.IsFailure)
            return changed.Error;

        ActiveChanged(logger, command.WarehouseId, command.IsActive);
        return Done.Value;
    }

    private async Task<Result<Done>> ChangeAsync(SetWarehouseActiveCommand command, CancellationToken ct)
    {
        var warehouse = await warehouses.GetForUpdateAsync(command.WarehouseId, expectedRowVersion: null, ct);
        if (warehouse is null)
            return WarehouseErrors.NotFound;

        if (command.IsActive)
        {
            if (!await warehouses.IsBranchActiveAsync(warehouse.BranchId, ct))
                return BranchErrors.NotActive;
            warehouse.Activate();
        }
        else
        {
            var occupancy = await stock.LockAndCheckWarehouseAsync(warehouse.Id, ct);
            if (occupancy != StockOccupancy.Free)
                return occupancy switch
                {
                    StockOccupancy.HoldsStock => WarehouseErrors.HoldsStock,
                    StockOccupancy.HasOpenDocuments => WarehouseErrors.HasOpenInventoryDocuments,
                    _ => WarehouseErrors.InventoryBusy,
                };
            warehouse.Deactivate();
        }

        var saved = await warehouses.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        return Done.Value;
    }

    [LoggerMessage(EventId = 2008, Level = LogLevel.Information, Message = "Warehouse {WarehouseId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid warehouseId, bool isActive);
}
