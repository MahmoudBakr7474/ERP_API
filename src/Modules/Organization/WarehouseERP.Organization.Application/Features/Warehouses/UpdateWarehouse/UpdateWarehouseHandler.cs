using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Warehouses.UpdateWarehouse;

// Scope is checked before existence, as for reads. Returns the new row version for the caller's next update.
public sealed partial class UpdateWarehouseHandler(
    IWarehouseRepository warehouses, ICurrentUser currentUser, ILogger<UpdateWarehouseHandler> logger)
    : ICommandHandler<UpdateWarehouseCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateWarehouseCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return WarehouseErrors.AccessDenied;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var warehouse = await warehouses.GetForUpdateAsync(command.WarehouseId, expected, ct);
        if (warehouse is null)
            return WarehouseErrors.NotFound;

        var updated = warehouse.Update(command.NameAr, command.NameEn, command.AllowsSelling);
        if (updated.IsFailure)
            return updated.Error;
        var saved = await warehouses.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        WarehouseUpdated(logger, warehouse.Id, warehouse.AllowsSelling);
        return Convert.ToBase64String(warehouse.RowVersion);
    }

    [LoggerMessage(EventId = 2007, Level = LogLevel.Information,
        Message = "Warehouse {WarehouseId} updated (allows selling: {AllowsSelling})")]
    private static partial void WarehouseUpdated(ILogger logger, Guid warehouseId, bool allowsSelling);
}
