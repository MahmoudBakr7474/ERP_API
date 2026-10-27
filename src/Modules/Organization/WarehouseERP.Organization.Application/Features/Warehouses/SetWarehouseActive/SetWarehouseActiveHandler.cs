using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Warehouses.SetWarehouseActive;

// Scope before existence. Reactivating needs an active branch, so an inactive branch never holds an active
// warehouse (SetBranchActive refuses the other direction).
public sealed partial class SetWarehouseActiveHandler(
    IWarehouseRepository warehouses, ICurrentUser currentUser, ILogger<SetWarehouseActiveHandler> logger)
    : ICommandHandler<SetWarehouseActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetWarehouseActiveCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return WarehouseErrors.AccessDenied;
        var warehouse = await warehouses.GetForUpdateAsync(command.WarehouseId, expectedRowVersion: null, ct);
        if (warehouse is null)
            return WarehouseErrors.NotFound;
        if (command.IsActive && !await warehouses.IsBranchActiveAsync(warehouse.BranchId, ct))
            return BranchErrors.NotActive;

        if (command.IsActive) warehouse.Activate();
        else warehouse.Deactivate();
        var saved = await warehouses.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ActiveChanged(logger, warehouse.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 2008, Level = LogLevel.Information, Message = "Warehouse {WarehouseId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid warehouseId, bool isActive);
}
