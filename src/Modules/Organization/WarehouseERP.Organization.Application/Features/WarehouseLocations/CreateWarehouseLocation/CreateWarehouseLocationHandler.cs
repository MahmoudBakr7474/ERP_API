using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.CreateWarehouseLocation;

// The location is added through its warehouse (the domain refuses an inactive one). One save point; the pre-check
// gives a clear error and the database still guards the code if two requests race.
public sealed partial class CreateWarehouseLocationHandler(
    IWarehouseRepository warehouses, ICurrentUser currentUser, ILogger<CreateWarehouseLocationHandler> logger)
    : ICommandHandler<CreateWarehouseLocationCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateWarehouseLocationCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return WarehouseErrors.AccessDenied;
        var warehouse = await warehouses.GetForUpdateAsync(command.WarehouseId, expectedRowVersion: null, ct);
        if (warehouse is null)
            return WarehouseErrors.NotFound;
        if (await warehouses.LocationCodeExistsAsync(warehouse.Id, command.Code.Trim(), ct))
            return WarehouseLocationErrors.CodeTaken;

        var added = warehouse.AddLocation(command.Code, command.NameAr, command.NameEn);
        if (added.IsFailure)
            return added.Error;
        var saved = await warehouses.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        var location = added.Value;
        LocationCreated(logger, location.Id, location.Code, warehouse.Id);
        return location.Id;
    }

    [LoggerMessage(EventId = 2009, Level = LogLevel.Information,
        Message = "Location {LocationId} ({LocationCode}) created in warehouse {WarehouseId}")]
    private static partial void LocationCreated(ILogger logger, Guid locationId, string locationCode, Guid warehouseId);
}
